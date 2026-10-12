using System.Diagnostics;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;

namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Fase oficial de Concurrencia basada en NBomber (C-07.4): modelo cerrado
/// <c>Simulation.IterationsForConstant(copies=N, iterations=N·Rondas)</c>, un GET por usuario por ronda,
/// sincronizado con <see cref="RoundCoordinator"/> (barrera de inicio/fin por ronda). Una sola ejecución
/// NBomber para todas las rondas, de modo que Mean/P95/RPS se calculan sobre las N·R solicitudes individuales.
/// NBomber queda confinado a Infrastructure.
/// </summary>
public sealed class ConcurrencyNBomberRunner : IConcurrencyRunner
{
    private const string PasoRequest = "request";
    private const int BarrierTimeoutMs = 30_000; // timeout técnico interno (no es parámetro del Usuario)

    private readonly ILogger<ConcurrencyNBomberRunner> _logger;

    public ConcurrencyNBomberRunner(ILogger<ConcurrencyNBomberRunner> logger)
    {
        _logger = logger;
    }

    public Task<ConcurrencyPhaseResult> RunAsync(ConcurrencyRunRequest request, CancellationToken cancellationToken = default)
        => Task.Run(() => Run(request, cancellationToken), CancellationToken.None);

    private ConcurrencyPhaseResult Run(ConcurrencyRunRequest request, CancellationToken cancellationToken)
    {
        var n = request.UsuariosConcurrentes;
        var r = request.Rondas;
        long esperadas = (long)n * r;
        var reportFolder = Directory.CreateTempSubdirectory("dnpc-concurrencia-").FullName;
        var coord = new RoundCoordinator(n, r, request.IntervaloRondasMs, BarrierTimeoutMs, request.OnRondaLiberada, request.OnRondaCompletada);

        // Cancelación: libera las barreras para desenredar rápido; la OCE se lanza al final.
        using var registro = cancellationToken.Register(coord.Abortar);

        // Pool de conexiones suficiente para no serializar las N solicitudes del lado del generador.
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = Math.Max(n, 1),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        using var http = new HttpClient(handler) { BaseAddress = request.BaseUrl, Timeout = TimeSpan.FromSeconds(100) };

        try
        {
            int iterations = checked(n * r);

            var scenario = Scenario.Create("concurrencia", async context =>
            {
                var instancia = context.ScenarioInfo.InstanceNumber;
                var ronda = coord.ProximaRonda(instancia);

                await coord.EsperarRondaPermitidaAsync(ronda, cancellationToken);
                if (coord.Faulted)
                {
                    return Response.Fail(statusCode: "sync");
                }

                // El request se prepara ANTES de la barrera para reducir el trabajo local tras la liberación.
                var mensaje = new HttpRequestMessage(HttpMethod.Get, request.RutaObjetivo);

                await coord.BarreraInicioAsync(ronda, instancia, cancellationToken);
                if (coord.Faulted)
                {
                    mensaje.Dispose();
                    return Response.Fail(statusCode: "sync");
                }

                var respuesta = await Step.Run(PasoRequest, context, async () =>
                {
                    coord.RegistrarInicio(ronda, instancia, Stopwatch.GetTimestamp());
                    try
                    {
                        using var http2 = await http.SendAsync(mensaje);
                        return http2.IsSuccessStatusCode
                            ? Response.Ok(statusCode: ((int)http2.StatusCode).ToString())
                            : Response.Fail(statusCode: ((int)http2.StatusCode).ToString());
                    }
                    catch
                    {
                        return Response.Fail();
                    }
                    finally
                    {
                        mensaje.Dispose();
                    }
                });

                await coord.BarreraFinAsync(ronda, instancia, cancellationToken);
                return respuesta;
            })
            .WithoutWarmUp()
            .WithRestartIterationOnFail(false)
            .WithMaxFailCount(int.MaxValue)
            .WithLoadSimulations(Simulation.IterationsForConstant(copies: n, iterations: iterations));

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder(reportFolder)
                .WithReportFormats()
                .Run();

            cancellationToken.ThrowIfCancellationRequested();

            var step = stats.ScenarioStats[0].StepStats.First(s => s.StepName == PasoRequest);
            return new ConcurrencyPhaseResult(
                true, esperadas, step.Ok.Request.Count, step.Fail.Request.Count,
                step.Ok.Latency.MeanMs, step.Ok.Latency.Percent95, step.Ok.Request.RPS,
                coord.DesfaseInicioMaximoMs, coord.DesfaseInicioPromedioMs, coord.TimeoutSincronizacion);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar la fase de concurrencia con NBomber.");
            return new ConcurrencyPhaseResult(false, esperadas, 0, 0, 0, 0, 0,
                coord.DesfaseInicioMaximoMs, coord.DesfaseInicioPromedioMs, coord.TimeoutSincronizacion);
        }
        finally
        {
            handler.Dispose();
            try
            {
                Directory.Delete(reportFolder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Carpeta temporal de reportes.
            }
        }
    }
}
