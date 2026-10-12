using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

public enum FaseEjecucion
{
    Preparacion,
    Readiness,
    WarmUp,
    RampUp,
    Medicion,
    Finalizado
}

/// <summary>Información de cabecera que M-05 muestra sobre la configuración a ejecutar.</summary>
public sealed record ConfiguracionEjecutableInfo(
    int ConfiguracionId,
    string Nombre,
    string Proyecto,
    string TipoPrueba,
    string RutaObjetivo,
    int NumeroEjecuciones,
    string Estado,
    IReadOnlyList<string> Metricas,
    bool PuedeEjecutar,
    string? MotivoNoEjecutable);

public sealed record EjecutarEvaluacionRequest(int ConfiguracionId);

public sealed record EjecutarEvaluacionResult(bool Exito, string Mensaje, string EstadoFinal);

/// <summary>Resumen de una <c>Ejecucion</c> finalizada, para la bitácora/resultados de M-05.</summary>
public sealed record EjecucionResumen(
    string VersionDotNet,
    int NumeroEjecucion,
    int OrdenGlobal,
    string Estado,
    decimal? Cpu,
    decimal? RamMb,
    decimal? TiempoRespuestaMs,
    decimal? LatenciaP95Ms,
    decimal? ThroughputRps,
    decimal? TamanoMb,
    long SolicitudesExitosas,
    long SolicitudesFallidas);

/// <summary>Actualización de progreso del motor (como máximo ~1/seg para CPU/RAM).</summary>
public sealed record EvaluationExecutionProgress(
    string ConfiguracionEstado,
    FaseEjecucion? Fase = null,
    string? VersionActual = null,
    int? RepeticionActual = null,
    int OrdenGlobal = 0,
    int TotalPasos = 0,
    string? LogLine = null,
    decimal? CpuActual = null,
    decimal? RamActual = null,
    EjecucionResumen? EjecucionFinalizada = null);

/// <summary>
/// Persistencia requerida por CU-04: cargar la configuración completa y guardar estado/ejecuciones.
/// </summary>
public interface IEvaluationExecutionRepository
{
    /// <summary>
    /// Carga la configuración con Proyecto, Migración, TipoPrueba, Parámetros, Métricas y Ejecuciones.
    /// </summary>
    Task<ConfiguracionEvaluacion?> GetConfiguracionAsync(int configuracionId, CancellationToken cancellationToken = default);

    Task ActualizarEstadoConfiguracionAsync(int configuracionId, string estado, CancellationToken cancellationToken = default);

    /// <summary>Inserta una <c>Ejecucion</c> con sus condiciones y resultados en un único SaveChanges.</summary>
    Task AgregarEjecucionAsync(Ejecucion ejecucion, CancellationToken cancellationToken = default);
}

/// <summary>
/// CU-04 — Ejecutar prueba de rendimiento (RF-04, RF-05, RF-06, RF-14). C-07.1: solo Carga + CPU/RAM/Tiempo de respuesta.
/// </summary>
public interface IEjecutarEvaluacionService
{
    Task<ConfiguracionEjecutableInfo?> ObtenerInfoAsync(int configuracionId, CancellationToken cancellationToken = default);

    Task<EjecutarEvaluacionResult> EjecutarAsync(
        EjecutarEvaluacionRequest request,
        IProgress<EvaluationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
