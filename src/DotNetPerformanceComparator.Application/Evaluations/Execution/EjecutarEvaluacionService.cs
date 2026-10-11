using System.Globalization;
using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// CU-04 (C-07.1): orquesta la ejecución experimental de una configuración de tipo Carga, alternando
/// .NET 8 y .NET 10 de forma secuencial, y persiste Ejecucion/CondicionEjecucion/ResultadoMetrica.
/// La generación de carga, los procesos y el muestreo de recursos viven en Infrastructure.
/// </summary>
public sealed class EjecutarEvaluacionService : IEjecutarEvaluacionService
{
    public const int IntervaloMuestreoMs = 1000;

    /// <summary>Métricas soportadas por el motor en C-07.1.</summary>
    public static IReadOnlyList<string> MetricasSoportadas { get; } =
        [NombresMetrica.Cpu, NombresMetrica.MemoriaRam, NombresMetrica.TiempoRespuesta];

    private readonly IEvaluationExecutionRepository _repo;
    private readonly IEvaluationExecutionGate _gate;
    private readonly IExecutionWorkspacePreparer _preparer;
    private readonly ITargetAppLauncher _launcher;
    private readonly ILoadRunner _loadRunner;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EjecutarEvaluacionService> _logger;
    private readonly int _intervaloMuestreoMs;

    public EjecutarEvaluacionService(
        IEvaluationExecutionRepository repo,
        IEvaluationExecutionGate gate,
        IExecutionWorkspacePreparer preparer,
        ITargetAppLauncher launcher,
        ILoadRunner loadRunner,
        TimeProvider timeProvider,
        ILogger<EjecutarEvaluacionService> logger,
        int intervaloMuestreoMs = IntervaloMuestreoMs)
    {
        _repo = repo;
        _gate = gate;
        _preparer = preparer;
        _launcher = launcher;
        _loadRunner = loadRunner;
        _timeProvider = timeProvider;
        _logger = logger;
        _intervaloMuestreoMs = intervaloMuestreoMs;
    }

    public async Task<ConfiguracionEjecutableInfo?> ObtenerInfoAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        var config = await _repo.GetConfiguracionAsync(configuracionId, cancellationToken);
        if (config is null)
        {
            return null;
        }

        var metricas = config.ConfiguracionesMetrica.Select(cm => cm.Metrica.Nombre).ToList();
        var (puede, motivo) = EvaluarEjecutable(config, metricas);

        return new ConfiguracionEjecutableInfo(
            config.Id, config.Nombre, config.Proyecto.Nombre, config.TipoPrueba.Nombre, config.RutaObjetivo,
            config.NumeroEjecuciones, config.Estado, metricas, puede, motivo);
    }

    public async Task<EjecutarEvaluacionResult> EjecutarAsync(
        EjecutarEvaluacionRequest request,
        IProgress<EvaluationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_gate.TryAcquire())
        {
            return new EjecutarEvaluacionResult(false, "Ya existe una evaluación de rendimiento en ejecución.", string.Empty);
        }

        try
        {
            var config = await _repo.GetConfiguracionAsync(request.ConfiguracionId, cancellationToken);
            if (config is null)
            {
                return new EjecutarEvaluacionResult(false, "La configuración indicada no existe.", string.Empty);
            }

            var metricas = config.ConfiguracionesMetrica.Select(cm => cm.Metrica).ToList();
            var (puede, motivo) = EvaluarEjecutable(config, metricas.Select(m => m.Nombre).ToList());
            if (!puede)
            {
                return new EjecutarEvaluacionResult(false, motivo!, config.Estado);
            }

            var carga = LeerParametrosCarga(config);

            return await EjecutarInternoAsync(config, metricas, carga, progress, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<EjecutarEvaluacionResult> EjecutarInternoAsync(
        ConfiguracionEvaluacion config,
        IReadOnlyList<Metrica> metricas,
        ParametrosCarga carga,
        IProgress<EvaluationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        void Log(string linea) => progress?.Report(new EvaluationExecutionProgress(config.Estado, LogLine: linea));

        Reportar(progress, config.Estado, FaseEjecucion.Preparacion, linea: "Preparando el entorno de evaluación...");
        config.Estado = EstadosConfiguracionEvaluacion.EnEjecucion;
        await _repo.ActualizarEstadoConfiguracionAsync(config.Id, config.Estado, CancellationToken.None);
        Reportar(progress, config.Estado, FaseEjecucion.Preparacion);

        PreparedWorkspace? workspace = null;
        try
        {
            var preparacion = await _preparer.PrepareAsync(
                new PrepareWorkspaceRequest(config.Proyecto.RutaOriginal, config.Migracion.RutaMigrada), Log, cancellationToken);

            if (!preparacion.Success || preparacion.Workspace is null)
            {
                return await FinalizarConfiguracionAsync(config, EstadosConfiguracionEvaluacion.Fallida, progress,
                    $"No fue posible preparar las versiones para la evaluación. {preparacion.ErrorMessage}");
            }

            workspace = preparacion.Workspace;

            var secuencia = EvaluationExecutionPlan.BuildSequence(config.NumeroEjecuciones);
            var orden = 0;
            foreach (var paso in secuencia)
            {
                cancellationToken.ThrowIfCancellationRequested();
                orden++;
                var version = paso.VersionDotNet == VersionesDotNet.Net8 ? workspace.Net8 : workspace.Net10;

                var (completada, mensaje) = await EjecutarPasoAsync(config, metricas, carga, paso, version, orden, secuencia.Count, progress, cancellationToken);
                if (!completada)
                {
                    return await FinalizarConfiguracionAsync(config, EstadosConfiguracionEvaluacion.Fallida, progress,
                        $"La evaluación se detuvo: {mensaje}");
                }
            }

            return await FinalizarConfiguracionAsync(config, EstadosConfiguracionEvaluacion.Finalizada, progress,
                "La evaluación finalizó correctamente.");
        }
        catch (OperationCanceledException)
        {
            return await FinalizarConfiguracionAsync(config, EstadosConfiguracionEvaluacion.Cancelada, progress,
                "La evaluación fue cancelada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante la evaluación {ConfiguracionId}.", config.Id);
            return await FinalizarConfiguracionAsync(config, EstadosConfiguracionEvaluacion.Fallida, progress,
                "Ocurrió un error inesperado durante la evaluación.");
        }
        finally
        {
            if (workspace is not null)
            {
                await _preparer.CleanupAsync(workspace, Log);
            }
        }
    }

    private async Task<(bool Completada, string Mensaje)> EjecutarPasoAsync(
        ConfiguracionEvaluacion config,
        IReadOnlyList<Metrica> metricas,
        ParametrosCarga carga,
        PasoEjecucion paso,
        PreparedVersion version,
        int orden,
        int totalPasos,
        IProgress<EvaluationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        void Fase(FaseEjecucion fase, string? linea = null, decimal? cpu = null, decimal? ram = null) =>
            progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, fase,
                version.VersionDotNet, paso.NumeroEjecucion, orden, totalPasos, linea, cpu, ram));

        var inicio = _timeProvider.GetUtcNow().UtcDateTime;
        ITargetApp? app = null;
        try
        {
            Fase(FaseEjecucion.Readiness, $"[{version.VersionDotNet} · repetición {paso.NumeroEjecucion}] Iniciando aplicación objetivo...");
            app = await _launcher.StartAsync(version, cancellationToken);

            if (!await app.WaitUntilReadyAsync(config.RutaObjetivo, cancellationToken) || app.HasExited)
            {
                await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, 0, 0, null, CancellationToken.None);
                return (false, $"La aplicación {version.VersionDotNet} no respondió en {config.RutaObjetivo}.");
            }

            var baseUrl = app.BaseUrl;
            if (carga.WarmUpSegundos > 0)
            {
                Fase(FaseEjecucion.WarmUp, "Warm-up...");
                await _loadRunner.RunPhaseAsync(new LoadPhaseRequest(baseUrl, config.RutaObjetivo, 1, TimeSpan.FromSeconds(carga.WarmUpSegundos), carga.IntervaloSolicitudesMs, false), cancellationToken);
                if (app.HasExited)
                {
                    await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, 0, 0, null, CancellationToken.None);
                    return (false, $"La aplicación {version.VersionDotNet} terminó durante el warm-up.");
                }
            }

            if (carga.RampUpSegundos > 0)
            {
                Fase(FaseEjecucion.RampUp, "Ramp-up...");
                await _loadRunner.RunPhaseAsync(new LoadPhaseRequest(baseUrl, config.RutaObjetivo, carga.UsuariosVirtuales, TimeSpan.FromSeconds(carga.RampUpSegundos), carga.IntervaloSolicitudesMs, true), cancellationToken);
                if (app.HasExited)
                {
                    await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, 0, 0, null, CancellationToken.None);
                    return (false, $"La aplicación {version.VersionDotNet} terminó durante el ramp-up.");
                }
            }

            Fase(FaseEjecucion.Medicion, $"Midiendo durante {carga.DuracionSegundos} s...");
            var muestras = new List<ResourceSample>();
            using var muestreoCts = new CancellationTokenSource();
            var muestreo = MuestrearAsync(app, muestras, progress, version, paso, orden, totalPasos, muestreoCts.Token);

            LoadPhaseResult load;
            try
            {
                load = await _loadRunner.RunPhaseAsync(
                    new LoadPhaseRequest(baseUrl, config.RutaObjetivo, carga.UsuariosVirtuales, TimeSpan.FromSeconds(carga.DuracionSegundos), carga.IntervaloSolicitudesMs, false),
                    cancellationToken);
            }
            finally
            {
                muestreoCts.Cancel();
                await muestreo;
            }

            if (!load.Started || load.SolicitudesExitosas == 0 || app.HasExited)
            {
                await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, load.SolicitudesExitosas, load.SolicitudesFallidas, null, CancellationToken.None);
                return (false, $"La fase de medición de {version.VersionDotNet} no obtuvo solicitudes exitosas.");
            }

            var agg = ResourceAggregator.Aggregate(muestras, Environment.ProcessorCount);
            var resultados = ConstruirResultados(metricas, agg, (decimal)load.TiempoRespuestaMedioMs, out var faltante);
            if (faltante is not null)
            {
                await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, load.SolicitudesExitosas, load.SolicitudesFallidas, null, CancellationToken.None);
                return (false, $"No fue posible obtener la métrica {faltante} de {version.VersionDotNet}.");
            }

            var resumen = await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Completada, inicio, load.SolicitudesExitosas, load.SolicitudesFallidas, resultados, CancellationToken.None);
            progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, FaseEjecucion.Medicion,
                version.VersionDotNet, paso.NumeroEjecucion, orden, totalPasos,
                $"[{version.VersionDotNet} · repetición {paso.NumeroEjecucion}] Completada.", EjecucionFinalizada: resumen));
            return (true, "OK");
        }
        catch (OperationCanceledException)
        {
            await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Cancelada, inicio, 0, 0, null, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la ejecución {Version} #{Numero} de la configuración {ConfiguracionId}.", version.VersionDotNet, paso.NumeroEjecucion, config.Id);
            await PersistirEjecucionAsync(config, carga, paso, version, orden, EstadosEjecucion.Fallida, inicio, 0, 0, null, CancellationToken.None);
            return (false, $"Error inesperado al ejecutar {version.VersionDotNet}.");
        }
        finally
        {
            if (app is not null)
            {
                await app.DisposeAsync();
            }
        }
    }

    private async Task MuestrearAsync(
        ITargetApp app, List<ResourceSample> muestras, IProgress<EvaluationExecutionProgress>? progress,
        PreparedVersion version, PasoEjecucion paso, int orden, int totalPasos, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var muestra = app.Sample();
                if (muestra is not null)
                {
                    muestras.Add(muestra);
                    if (muestras.Count >= 2)
                    {
                        var parcial = ResourceAggregator.Aggregate(muestras.TakeLast(2).ToList(), Environment.ProcessorCount);
                        progress?.Report(new EvaluationExecutionProgress(EstadosConfiguracionEvaluacion.EnEjecucion, FaseEjecucion.Medicion,
                            version.VersionDotNet, paso.NumeroEjecucion, orden, totalPasos, CpuActual: parcial.CpuPorcentaje, RamActual: parcial.MemoriaMb));
                    }
                }

                await Task.Delay(_intervaloMuestreoMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Fin normal del muestreo.
        }
    }

    private async Task<EjecucionResumen> PersistirEjecucionAsync(
        ConfiguracionEvaluacion config, ParametrosCarga carga, PasoEjecucion paso, PreparedVersion version,
        int orden, string estado, DateTime inicio, long exitosas, long fallidas,
        IReadOnlyList<(int MetricaId, decimal Valor)>? resultados, CancellationToken cancellationToken)
    {
        var ejecucion = new Ejecucion
        {
            ConfiguracionId = config.Id,
            VersionDotNet = version.VersionDotNet,
            NumeroEjecucion = paso.NumeroEjecucion,
            FechaInicio = inicio,
            FechaFin = _timeProvider.GetUtcNow().UtcDateTime,
            Estado = estado,
            Condiciones = ConstruirCondiciones(config, carga, version, orden, exitosas, fallidas),
            Resultados = (resultados ?? []).Select(r => new ResultadoMetrica { MetricaId = r.MetricaId, Valor = r.Valor }).ToList()
        };

        try
        {
            await _repo.AgregarEjecucionAsync(ejecucion, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible persistir la ejecución {Version} #{Numero}.", version.VersionDotNet, paso.NumeroEjecucion);
        }

        decimal? ValorDe(string nombre) => resultados is null
            ? null
            : BuscarValor(config, nombre, resultados);

        return new EjecucionResumen(version.VersionDotNet, paso.NumeroEjecucion, orden, estado,
            ValorDe(NombresMetrica.Cpu), ValorDe(NombresMetrica.MemoriaRam), ValorDe(NombresMetrica.TiempoRespuesta), exitosas, fallidas);
    }

    private static decimal? BuscarValor(ConfiguracionEvaluacion config, string nombre, IReadOnlyList<(int MetricaId, decimal Valor)> resultados)
    {
        var metrica = config.ConfiguracionesMetrica.Select(cm => cm.Metrica).FirstOrDefault(m => m.Nombre == nombre);
        if (metrica is null)
        {
            return null;
        }

        foreach (var (metricaId, valor) in resultados)
        {
            if (metricaId == metrica.Id)
            {
                return valor;
            }
        }

        return null;
    }

    private List<CondicionEjecucion> ConstruirCondiciones(
        ConfiguracionEvaluacion config, ParametrosCarga carga, PreparedVersion version, int orden, long exitosas, long fallidas)
    {
        (string Clave, string Valor)[] condiciones =
        [
            (ClavesCondicionEjecucion.TipoPrueba, config.TipoPrueba.Nombre),
            (ClavesCondicionEjecucion.MetodoHttp, ConfigurarEvaluacionService.MetodoHttp),
            (ClavesCondicionEjecucion.RutaObjetivo, config.RutaObjetivo),
            (ClavesParametroCarga.UsuariosVirtuales, carga.UsuariosVirtuales.ToString(CultureInfo.InvariantCulture)),
            (ClavesParametroCarga.DuracionSegundos, carga.DuracionSegundos.ToString(CultureInfo.InvariantCulture)),
            (ClavesParametroCarga.WarmUpSegundos, carga.WarmUpSegundos.ToString(CultureInfo.InvariantCulture)),
            (ClavesParametroCarga.RampUpSegundos, carga.RampUpSegundos.ToString(CultureInfo.InvariantCulture)),
            (ClavesParametroCarga.IntervaloSolicitudesMs, carga.IntervaloSolicitudesMs.ToString(CultureInfo.InvariantCulture)),
            (ClavesCondicionEjecucion.ConfiguracionCompilacion, "Release"),
            (ClavesCondicionEjecucion.TargetFramework, version.TargetFramework),
            (ClavesCondicionEjecucion.SdkVersion, version.SdkVersion),
            (ClavesCondicionEjecucion.Environment, ClavesCondicionEjecucion.EntornoObjetivo),
            (ClavesCondicionEjecucion.IntervaloMuestreoMs, _intervaloMuestreoMs.ToString(CultureInfo.InvariantCulture)),
            (ClavesCondicionEjecucion.OrdenGlobal, orden.ToString(CultureInfo.InvariantCulture)),
            (ClavesCondicionEjecucion.SolicitudesExitosas, exitosas.ToString(CultureInfo.InvariantCulture)),
            (ClavesCondicionEjecucion.SolicitudesFallidas, fallidas.ToString(CultureInfo.InvariantCulture))
        ];

        return condiciones.Select(c => new CondicionEjecucion { Clave = c.Clave, Valor = c.Valor }).ToList();
    }

    /// <summary>Construye los resultados de las métricas seleccionadas; <paramref name="faltante"/> indica cuál no pudo obtenerse.</summary>
    private static List<(int MetricaId, decimal Valor)> ConstruirResultados(
        IReadOnlyList<Metrica> metricas, ResourceAggregates agg, decimal tiempoRespuestaMs, out string? faltante)
    {
        faltante = null;
        var resultados = new List<(int, decimal)>();
        foreach (var metrica in metricas)
        {
            decimal? valor = metrica.Nombre switch
            {
                NombresMetrica.Cpu => agg.CpuPorcentaje,
                NombresMetrica.MemoriaRam => agg.MemoriaMb,
                NombresMetrica.TiempoRespuesta => tiempoRespuestaMs,
                _ => null
            };

            if (valor is null)
            {
                faltante = metrica.Nombre;
                return [];
            }

            resultados.Add((metrica.Id, valor.Value));
        }

        return resultados;
    }

    private async Task<EjecutarEvaluacionResult> FinalizarConfiguracionAsync(
        ConfiguracionEvaluacion config, string estado, IProgress<EvaluationExecutionProgress>? progress, string mensaje)
    {
        config.Estado = estado;
        await _repo.ActualizarEstadoConfiguracionAsync(config.Id, estado, CancellationToken.None);
        progress?.Report(new EvaluationExecutionProgress(estado, FaseEjecucion.Finalizado, LogLine: mensaje));
        return new EjecutarEvaluacionResult(estado == EstadosConfiguracionEvaluacion.Finalizada, mensaje, estado);
    }

    private static void Reportar(IProgress<EvaluationExecutionProgress>? progress, string estado, FaseEjecucion fase, string? linea = null) =>
        progress?.Report(new EvaluationExecutionProgress(estado, fase, LogLine: linea));

    private static (bool Puede, string? Motivo) EvaluarEjecutable(ConfiguracionEvaluacion config, IReadOnlyList<string> metricas)
    {
        if (config.Estado != EstadosConfiguracionEvaluacion.Configurada)
        {
            return (false, $"La configuración no está en estado Configurada (estado actual: {config.Estado}). Cree otra configuración para volver a evaluar.");
        }

        if (config.Ejecuciones.Count > 0)
        {
            return (false, "La configuración ya fue ejecutada. Cree otra configuración para volver a evaluar.");
        }

        if (config.TipoPrueba.Nombre != TiposPruebaConocidos.Carga)
        {
            return (false, "Este tipo de prueba todavía no está habilitado en el motor de ejecución.");
        }

        var noSoportadas = metricas.Where(m => !MetricasSoportadas.Contains(m)).ToList();
        if (noSoportadas.Count > 0)
        {
            return (false, $"El motor de ejecución todavía no soporta la(s) métrica(s): {string.Join(", ", noSoportadas)}.");
        }

        return (true, null);
    }

    private static ParametrosCarga LeerParametrosCarga(ConfiguracionEvaluacion config)
    {
        int Leer(string clave) => int.Parse(
            config.Parametros.First(p => p.Clave == clave).Valor, CultureInfo.InvariantCulture);

        return new ParametrosCarga(
            Leer(ClavesParametroCarga.UsuariosVirtuales),
            Leer(ClavesParametroCarga.DuracionSegundos),
            Leer(ClavesParametroCarga.WarmUpSegundos),
            Leer(ClavesParametroCarga.RampUpSegundos),
            Leer(ClavesParametroCarga.IntervaloSolicitudesMs));
    }

    private sealed record ParametrosCarga(int UsuariosVirtuales, int DuracionSegundos, int WarmUpSegundos, int RampUpSegundos, int IntervaloSolicitudesMs);
}
