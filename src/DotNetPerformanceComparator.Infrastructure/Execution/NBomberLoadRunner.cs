using DotNetPerformanceComparator.Application.Evaluations.Execution;
using Microsoft.Extensions.Logging;
using NBomber.Contracts;
using NBomber.CSharp;

namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Motor de carga HTTP GET basado en NBomber. Mide solo la solicitud HTTP (paso "request"); el
/// intervalo entre solicitudes se aplica con un delay fuera del paso, por lo que no entra en la latencia.
/// NBomber queda confinado a Infrastructure: Application solo recibe <see cref="LoadPhaseResult"/>.
/// </summary>
public sealed class NBomberLoadRunner : ILoadRunner
{
    private const string PasoRequest = "request";

    private readonly ILogger<NBomberLoadRunner> _logger;

    public NBomberLoadRunner(ILogger<NBomberLoadRunner> logger)
    {
        _logger = logger;
    }

    public Task<LoadPhaseResult> RunPhaseAsync(LoadPhaseRequest request, CancellationToken cancellationToken = default)
        => Task.Run(() => RunPhase(request), CancellationToken.None);

    private LoadPhaseResult RunPhase(LoadPhaseRequest request)
    {
        var reportFolder = Directory.CreateTempSubdirectory("dnpc-nbomber-").FullName;
        try
        {
            using var http = new HttpClient { BaseAddress = request.BaseUrl };
            var ruta = request.RutaObjetivo;

            var scenario = Scenario.Create("carga", async context =>
            {
                var step = await Step.Run(PasoRequest, context, async () =>
                {
                    try
                    {
                        using var respuesta = await http.GetAsync(ruta);
                        return respuesta.IsSuccessStatusCode
                            ? Response.Ok(statusCode: ((int)respuesta.StatusCode).ToString())
                            : Response.Fail(statusCode: ((int)respuesta.StatusCode).ToString());
                    }
                    catch
                    {
                        return Response.Fail();
                    }
                });

                if (request.IntervaloSolicitudesMs > 0)
                {
                    await Task.Delay(request.IntervaloSolicitudesMs);
                }

                return step;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(request.Ramping
                ? Simulation.RampingConstant(copies: request.Usuarios, during: request.Duracion)
                : Simulation.KeepConstant(copies: request.Usuarios, during: request.Duracion));

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder(reportFolder)
                .WithReportFormats()
                .Run();

            // Estadísticas específicas del Step "request" (no del escenario): el delay de pacing ocurre
            // fuera del Step, por lo que Mean/P95 reflejan solo la solicitud HTTP. Solo bloque Ok.
            var step = stats.ScenarioStats[0].StepStats.First(s => s.StepName == PasoRequest);
            return new LoadPhaseResult(
                true,
                step.Ok.Request.Count,
                step.Fail.Request.Count,
                step.Ok.Latency.MeanMs,
                step.Ok.Latency.Percent95,
                step.Ok.Request.RPS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar la fase de carga con NBomber.");
            return new LoadPhaseResult(false, 0, 0, 0, 0, 0);
        }
        finally
        {
            try
            {
                Directory.Delete(reportFolder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Carpeta temporal de reportes: se limpiará por el sistema si no puede borrarse ahora.
            }
        }
    }
}
