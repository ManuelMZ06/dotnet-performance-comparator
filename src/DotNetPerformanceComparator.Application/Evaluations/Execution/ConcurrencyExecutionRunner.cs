using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>Contexto de una ejecución de Concurrencia para un paso versión+repetición.</summary>
public sealed record ConcurrencyExecutionContext(
    string RutaObjetivo,
    ITargetApp App,
    PreparedVersion Version,
    PasoEjecucion Paso,
    int Orden,
    int TotalPasos,
    IReadOnlyList<Metrica> MetricasSeleccionadas,
    ParametrosConcurrencia Parametros,
    int IntervaloMuestreoMs);

/// <summary>Resultado de ejecutar la fase de Concurrencia de una Ejecucion.</summary>
public sealed record ConcurrencyExecutionOutcome(
    string EstadoEjecucion,
    IReadOnlyList<(int MetricaId, decimal Valor)>? Resultados,
    string Mensaje,
    long SolicitudesEsperadas,
    long SolicitudesExitosas,
    long SolicitudesFallidas,
    double DesfaseInicioMaximoMs,
    double DesfaseInicioPromedioMs);

public interface IConcurrencyExecutionRunner
{
    Task<ConcurrencyExecutionOutcome> EjecutarAsync(
        ConcurrencyExecutionContext contexto, IProgress<EvaluationExecutionProgress>? progress, CancellationToken cancellationToken);
}

/// <summary>
/// Coordina la fase de Concurrencia: warm-up único (ILoadRunner, descartado) → ejecución oficial de rondas
/// sincronizadas (<see cref="IConcurrencyRunner"/>) con muestreo CPU/RAM durante toda la ventana (incluye
/// una muestra inicial y otra final para ser válida aun en pruebas muy cortas), y construcción de resultados.
/// Mantiene a NBomber aislado en Infrastructure.
/// </summary>
public sealed class ConcurrencyExecutionRunner : IConcurrencyExecutionRunner
{
    private readonly ILoadRunner _loadRunner;
    private readonly IConcurrencyRunner _concurrencyRunner;

    public ConcurrencyExecutionRunner(ILoadRunner loadRunner, IConcurrencyRunner concurrencyRunner)
    {
        _loadRunner = loadRunner;
        _concurrencyRunner = concurrencyRunner;
    }

    public async Task<ConcurrencyExecutionOutcome> EjecutarAsync(
        ConcurrencyExecutionContext ctx, IProgress<EvaluationExecutionProgress>? progress, CancellationToken cancellationToken)
    {
        var app = ctx.App;
        var p = ctx.Parametros;
        var esperadas = (long)p.UsuariosConcurrentes * p.Rondas;

        void Reportar(FaseEjecucion fase, string? linea, int? ronda = null) =>
            progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, fase,
                ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, ctx.Orden, ctx.TotalPasos, linea,
                RondaActual: ronda, TotalRondas: p.Rondas, UsuariosConcurrentes: p.UsuariosConcurrentes));

        ConcurrencyExecutionOutcome Fallida(string mensaje) =>
            new(EstadosEjecucion.Fallida, null, mensaje, esperadas, 0, 0, 0, 0);

        // Warm-up único (resultado descartado).
        if (p.WarmUpSegundos > 0)
        {
            Reportar(FaseEjecucion.WarmUp, "Warm-up...");
            await _loadRunner.RunPhaseAsync(
                new LoadPhaseRequest(app.BaseUrl, ctx.RutaObjetivo, 1, TimeSpan.FromSeconds(p.WarmUpSegundos), 0, Ramping: false),
                cancellationToken);
            if (app.HasExited)
            {
                return Fallida("La aplicación objetivo terminó durante el warm-up.");
            }
        }

        Reportar(FaseEjecucion.Concurrencia, $"Ejecutando {p.Rondas} rondas de {p.UsuariosConcurrentes} usuarios sincronizados...");

        // Ventana de recursos: muestra inicial + muestreo periódico + muestra final (válida aun si dura < 1 s).
        var muestras = new List<ResourceSample>();
        var inicial = app.Sample();
        if (inicial is not null) muestras.Add(inicial);

        using var muestreoCts = new CancellationTokenSource();
        var muestreo = ResourceSampler.RunAsync(app, muestras, ctx.IntervaloMuestreoMs, Environment.ProcessorCount,
            parcial => progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, FaseEjecucion.Concurrencia,
                ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, ctx.Orden, ctx.TotalPasos,
                CpuActual: parcial.CpuPorcentaje, RamActual: parcial.MemoriaMb, TotalRondas: p.Rondas, UsuariosConcurrentes: p.UsuariosConcurrentes)),
            muestreoCts.Token);

        ConcurrencyPhaseResult res;
        try
        {
            res = await _concurrencyRunner.RunAsync(new ConcurrencyRunRequest(
                app.BaseUrl, ctx.RutaObjetivo, p.UsuariosConcurrentes, p.Rondas, p.IntervaloRondasMs,
                OnRondaLiberada: ronda => Reportar(FaseEjecucion.Concurrencia, null, ronda),
                OnRondaCompletada: (ronda, skew) => Reportar(FaseEjecucion.Concurrencia,
                    $"Ronda {ronda} sincronizada; desfase de inicio {skew:0.##} ms.", ronda)),
                cancellationToken);
        }
        finally
        {
            muestreoCts.Cancel();
            await muestreo;
        }

        var final = app.Sample();
        if (final is not null) muestras.Add(final);

        if (!res.Started || res.TimeoutSincronizacion)
        {
            return Fallida("No fue posible sincronizar todos los usuarios virtuales de la ronda.");
        }

        if (app.HasExited)
        {
            return Fallida("El proceso objetivo terminó durante la prueba de concurrencia.");
        }

        var total = res.SolicitudesExitosas + res.SolicitudesFallidas;
        if (total != res.SolicitudesEsperadas || res.SolicitudesEsperadas != esperadas)
        {
            return Fallida($"Número de solicitudes inconsistente (esperadas {esperadas}, ejecutadas {total}).");
        }

        if (res.SolicitudesExitosas == 0)
        {
            return new ConcurrencyExecutionOutcome(EstadosEjecucion.Fallida, null,
                "Ninguna solicitud fue exitosa durante la prueba de concurrencia.",
                esperadas, 0, res.SolicitudesFallidas, res.DesfaseInicioMaximoMs, res.DesfaseInicioPromedioMs);
        }

        var agg = ResourceAggregator.Aggregate(muestras, Environment.ProcessorCount);
        var inputs = new MetricInputs(agg.CpuPorcentaje, agg.MemoriaMb,
            res.TiempoRespuestaMedioMs, res.LatenciaP95Ms, res.ThroughputRps, ctx.Version.PublishedSizeBytes);
        var (resultados, faltante) = EvaluationMetricResultBuilder.Build(ctx.MetricasSeleccionadas, inputs);
        if (faltante is not null)
        {
            return new ConcurrencyExecutionOutcome(EstadosEjecucion.Fallida, null,
                $"No fue posible obtener la métrica {faltante}.", esperadas,
                res.SolicitudesExitosas, res.SolicitudesFallidas, res.DesfaseInicioMaximoMs, res.DesfaseInicioPromedioMs);
        }

        return new ConcurrencyExecutionOutcome(EstadosEjecucion.Completada, resultados,
            "La prueba de concurrencia finalizó correctamente.", esperadas,
            res.SolicitudesExitosas, res.SolicitudesFallidas, res.DesfaseInicioMaximoMs, res.DesfaseInicioPromedioMs);
    }
}
