using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>Parámetros de una prueba de Estrés escalonada (C-07.3).</summary>
public sealed record ParametrosEstres(
    int UsuariosIniciales,
    int IncrementoUsuarios,
    int UsuariosMaximos,
    int DuracionNivelSegundos,
    int WarmUpSegundos,
    int IntervaloSolicitudesMs);

/// <summary>Contexto de una ejecución de Estrés ya creada (EnProceso) para un paso versión+repetición.</summary>
public sealed record StressExecutionContext(
    int EjecucionId,
    string RutaObjetivo,
    ITargetApp App,
    PreparedVersion Version,
    PasoEjecucion Paso,
    int Orden,
    int TotalPasos,
    IReadOnlyList<Metrica> MetricasSeleccionadas,
    ParametrosEstres Parametros,
    int IntervaloMuestreoMs);

/// <summary>Resultado de ejecutar todos los niveles de una Ejecucion de Estrés.</summary>
/// <param name="EstadoEjecucion">Completada / CompletadaConSaturacion / Fallida (técnico).</param>
/// <param name="ResumenResultados">Resultados de resumen (solo si alcanzó el nivel máximo); null en saturación/fallo.</param>
/// <param name="EsFalloTecnico">true si debe detenerse toda la evaluación (fallo técnico).</param>
public sealed record StressExecutionOutcome(
    string EstadoEjecucion,
    IReadOnlyList<(int MetricaId, decimal Valor)>? ResumenResultados,
    bool EsFalloTecnico,
    string Mensaje);

/// <summary>
/// Ejecuta la secuencia de niveles de Estrés sobre un proceso objetivo ya iniciado y listo (readiness
/// correcto): warm-up único → niveles <c>KeepConstant</c> (sin ramp-up) sobre el mismo proceso, con monitor
/// de recursos reiniciado por nivel y persistencia incremental de cada <c>NivelEstres</c>. Mantiene a NBomber
/// aislado en Infrastructure (solo usa <see cref="ILoadRunner"/>). Extraído de <c>EjecutarEvaluacionService</c>
/// para no inflar el coordinador general.
/// </summary>
public interface IStressExecutionRunner
{
    Task<StressExecutionOutcome> EjecutarNivelesAsync(
        StressExecutionContext contexto, IProgress<EvaluationExecutionProgress>? progress, CancellationToken cancellationToken);
}

public sealed class StressExecutionRunner : IStressExecutionRunner
{
    private readonly IEvaluationExecutionRepository _repo;
    private readonly ILoadRunner _loadRunner;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StressExecutionRunner> _logger;

    public StressExecutionRunner(
        IEvaluationExecutionRepository repo, ILoadRunner loadRunner, TimeProvider timeProvider, ILogger<StressExecutionRunner> logger)
    {
        _repo = repo;
        _loadRunner = loadRunner;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<StressExecutionOutcome> EjecutarNivelesAsync(
        StressExecutionContext ctx, IProgress<EvaluationExecutionProgress>? progress, CancellationToken cancellationToken)
    {
        var app = ctx.App;
        var p = ctx.Parametros;

        // Métricas por nivel = variables bajo presión (sin Tamaño, que no depende de los usuarios).
        var metricasNivel = ctx.MetricasSeleccionadas.Where(m => m.Nombre != NombresMetrica.TamanoAplicacion).ToList();
        // Para un nivel saturado (0 exitosas) solo se persisten CPU/RAM confiables; nunca Mean/P95/RPS=0.
        var metricasCpuRam = metricasNivel.Where(m => m.Nombre is NombresMetrica.Cpu or NombresMetrica.MemoriaRam).ToList();

        var niveles = StressLevelPlan.Generar(p.UsuariosIniciales, p.IncrementoUsuarios, p.UsuariosMaximos);

        // Warm-up único por Ejecucion (no por nivel); runner independiente, resultado descartado.
        if (p.WarmUpSegundos > 0)
        {
            Reportar(progress, ctx, FaseEjecucion.WarmUp, $"[{ctx.Version.VersionDotNet} · repetición {ctx.Paso.NumeroEjecucion}] Warm-up...");
            await _loadRunner.RunPhaseAsync(
                new LoadPhaseRequest(app.BaseUrl, ctx.RutaObjetivo, 1, TimeSpan.FromSeconds(p.WarmUpSegundos), p.IntervaloSolicitudesMs, Ramping: false),
                cancellationToken);

            if (app.HasExited)
            {
                return await SaturarPrimerNivelAsync(ctx, niveles, progress,
                    "El proceso objetivo terminó durante el warm-up de estrés.");
            }
        }

        ResourceAggregates? ultimoAgg = null;
        LoadPhaseResult? ultimoLoad = null;
        var numero = 0;

        foreach (var usuarios in niveles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            numero++;

            var inicio = _timeProvider.GetUtcNow().UtcDateTime;
            var nivel = new NivelEstres
            {
                EjecucionId = ctx.EjecucionId,
                NumeroNivel = numero,
                UsuariosObjetivo = usuarios,
                FechaInicio = inicio,
                Estado = EstadosNivelEstres.EnProceso
            };
            var nivelId = await _repo.AgregarNivelEnProcesoAsync(nivel, CancellationToken.None);

            Reportar(progress, ctx, FaseEjecucion.NivelEstres,
                $"[{ctx.Version.VersionDotNet} · repetición {ctx.Paso.NumeroEjecucion}] Nivel {numero} de {niveles.Count}: {usuarios} usuarios.",
                nivelActual: numero, totalNiveles: niveles.Count, usuariosNivel: usuarios);

            if (app.HasExited)
            {
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Saturado, _timeProvider.GetUtcNow().UtcDateTime,
                    0, 0, "El proceso objetivo terminó durante el nivel de estrés.", [], CancellationToken.None);
                ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Saturado, null, 0, 0);
                return new StressExecutionOutcome(EstadosEjecucion.CompletadaConSaturacion, null, false,
                    $"Saturación detectada en {usuarios} usuarios (el proceso objetivo terminó).");
            }

            var muestras = new List<ResourceSample>();
            using var muestreoCts = new CancellationTokenSource();
            var muestreo = ResourceSampler.RunAsync(app, muestras, ctx.IntervaloMuestreoMs, Environment.ProcessorCount,
                parcial => progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion,
                    FaseEjecucion.NivelEstres, ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, ctx.Orden, ctx.TotalPasos,
                    CpuActual: parcial.CpuPorcentaje, RamActual: parcial.MemoriaMb,
                    NivelActual: numero, TotalNiveles: niveles.Count, UsuariosNivel: usuarios)),
                muestreoCts.Token);

            LoadPhaseResult load;
            try
            {
                load = await _loadRunner.RunPhaseAsync(
                    new LoadPhaseRequest(app.BaseUrl, ctx.RutaObjetivo, usuarios, TimeSpan.FromSeconds(p.DuracionNivelSegundos), p.IntervaloSolicitudesMs, Ramping: false),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                muestreoCts.Cancel();
                await muestreo;
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Cancelado, _timeProvider.GetUtcNow().UtcDateTime,
                    0, 0, "Nivel cancelado.", [], CancellationToken.None);
                throw;
            }
            finally
            {
                muestreoCts.Cancel();
                await muestreo;
            }

            var fin = _timeProvider.GetUtcNow().UtcDateTime;

            // Cancelación solicitada durante la medición del nivel: el nivel activo se marca Cancelado
            // (sin resultados) aunque NBomber haya terminado su fase; la OCE propaga a la Ejecucion/Config.
            if (cancellationToken.IsCancellationRequested)
            {
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Cancelado, fin,
                    load.SolicitudesExitosas, load.SolicitudesFallidas, "Nivel cancelado durante la medición.", [], CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!load.Started)
            {
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Fallido, fin,
                    load.SolicitudesExitosas, load.SolicitudesFallidas,
                    "No fue posible ejecutar el nivel (error del motor de carga).", [], CancellationToken.None);
                ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Fallido, null, load.SolicitudesExitosas, load.SolicitudesFallidas);
                return new StressExecutionOutcome(EstadosEjecucion.Fallida, null, true,
                    $"Fallo técnico del motor de carga en el nivel de {usuarios} usuarios.");
            }

            if (app.HasExited)
            {
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Saturado, fin,
                    load.SolicitudesExitosas, load.SolicitudesFallidas,
                    "El proceso objetivo terminó durante el nivel de estrés.", [], CancellationToken.None);
                ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Saturado, null, load.SolicitudesExitosas, load.SolicitudesFallidas);
                return new StressExecutionOutcome(EstadosEjecucion.CompletadaConSaturacion, null, false,
                    $"Saturación detectada en {usuarios} usuarios (el proceso objetivo terminó).");
            }

            var agg = ResourceAggregator.Aggregate(muestras, Environment.ProcessorCount);

            if (load.SolicitudesExitosas == 0)
            {
                // Saturación observada: la ventana se ejecutó pero no hubo respuestas exitosas.
                var parciales = EvaluationMetricResultBuilder.BuildBestEffort(
                    metricasCpuRam, Inputs(agg, load, ctx.Version.PublishedSizeBytes));
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Saturado, fin,
                    load.SolicitudesExitosas, load.SolicitudesFallidas,
                    "El nivel completó su ventana sin solicitudes exitosas.", parciales, CancellationToken.None);
                ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Saturado, (agg, load), load.SolicitudesExitosas, load.SolicitudesFallidas);
                return new StressExecutionOutcome(EstadosEjecucion.CompletadaConSaturacion, null, false,
                    $"Saturación detectada en {usuarios} usuarios (0 solicitudes exitosas).");
            }

            var inputs = Inputs(agg, load, ctx.Version.PublishedSizeBytes);
            var (resultados, faltante) = EvaluationMetricResultBuilder.Build(metricasNivel, inputs);
            if (faltante is not null)
            {
                await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Fallido, fin,
                    load.SolicitudesExitosas, load.SolicitudesFallidas,
                    $"Estadísticas incoherentes para la métrica {faltante} pese a existir solicitudes.", [], CancellationToken.None);
                ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Fallido, (agg, load), load.SolicitudesExitosas, load.SolicitudesFallidas);
                return new StressExecutionOutcome(EstadosEjecucion.Fallida, null, true,
                    $"Estadísticas incoherentes en el nivel de {usuarios} usuarios.");
            }

            await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Completado, fin,
                load.SolicitudesExitosas, load.SolicitudesFallidas, null, resultados, CancellationToken.None);
            ReportarNivel(progress, ctx, numero, usuarios, EstadosNivelEstres.Completado, (agg, load), load.SolicitudesExitosas, load.SolicitudesFallidas);

            ultimoAgg = agg;
            ultimoLoad = load;
        }

        // Alcanzó el nivel máximo sin saturación: resumen = valores del último nivel + Tamaño.
        var resumenInputs = Inputs(ultimoAgg!, ultimoLoad!, ctx.Version.PublishedSizeBytes);
        var (resumen, faltanteResumen) = EvaluationMetricResultBuilder.Build(ctx.MetricasSeleccionadas, resumenInputs);
        if (faltanteResumen is not null)
        {
            return new StressExecutionOutcome(EstadosEjecucion.Fallida, null, true,
                $"No fue posible construir el resumen de la ejecución (métrica {faltanteResumen}).");
        }

        return new StressExecutionOutcome(EstadosEjecucion.Completada, resumen, false, "La prueba de estrés alcanzó el nivel máximo.");
    }

    private async Task<StressExecutionOutcome> SaturarPrimerNivelAsync(
        StressExecutionContext ctx, IReadOnlyList<int> niveles, IProgress<EvaluationExecutionProgress>? progress, string detalle)
    {
        var usuarios = niveles.Count > 0 ? niveles[0] : ctx.Parametros.UsuariosIniciales;
        var ahora = _timeProvider.GetUtcNow().UtcDateTime;
        var nivelId = await _repo.AgregarNivelEnProcesoAsync(new NivelEstres
        {
            EjecucionId = ctx.EjecucionId, NumeroNivel = 1, UsuariosObjetivo = usuarios, FechaInicio = ahora, Estado = EstadosNivelEstres.EnProceso
        }, CancellationToken.None);
        await _repo.CompletarNivelAsync(nivelId, EstadosNivelEstres.Saturado, ahora, 0, 0, detalle, [], CancellationToken.None);
        ReportarNivel(progress, ctx, 1, usuarios, EstadosNivelEstres.Saturado, null, 0, 0);
        return new StressExecutionOutcome(EstadosEjecucion.CompletadaConSaturacion, null, false, detalle);
    }

    private static MetricInputs Inputs(ResourceAggregates agg, LoadPhaseResult load, long publishedSizeBytes) =>
        new(agg.CpuPorcentaje, agg.MemoriaMb, load.TiempoRespuestaMedioMs, load.LatenciaP95Ms, load.ThroughputExitosoRps, publishedSizeBytes);

    private static void Reportar(
        IProgress<EvaluationExecutionProgress>? progress, StressExecutionContext ctx, FaseEjecucion fase, string? linea,
        int? nivelActual = null, int? totalNiveles = null, int? usuariosNivel = null) =>
        progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, fase,
            ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, ctx.Orden, ctx.TotalPasos, linea,
            NivelActual: nivelActual, TotalNiveles: totalNiveles, UsuariosNivel: usuariosNivel));

    private void ReportarNivel(
        IProgress<EvaluationExecutionProgress>? progress, StressExecutionContext ctx, int numero, int usuarios,
        string estado, (ResourceAggregates Agg, LoadPhaseResult Load)? datos, long exitosas, long fallidas)
    {
        decimal? cpu = datos?.Agg.CpuPorcentaje;
        decimal? ram = datos?.Agg.MemoriaMb;
        decimal? mean = null, p95 = null, rps = null;
        if (datos is { } d && estado == EstadosNivelEstres.Completado)
        {
            mean = ToDec(d.Load.TiempoRespuestaMedioMs);
            p95 = ToDec(d.Load.LatenciaP95Ms);
            rps = ToDec(d.Load.ThroughputExitosoRps);
        }

        var resumen = new NivelEstresResumen(ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, numero, usuarios, estado,
            cpu, ram, mean, p95, rps, exitosas, fallidas);
        progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, FaseEjecucion.NivelEstres,
            ctx.Version.VersionDotNet, ctx.Paso.NumeroEjecucion, ctx.Orden, ctx.TotalPasos,
            NivelActual: numero, TotalNiveles: null, UsuariosNivel: usuarios, NivelFinalizado: resumen));
    }

    private static decimal? ToDec(double v) => double.IsFinite(v) && v >= 0 && v <= (double)decimal.MaxValue ? (decimal)v : null;
}
