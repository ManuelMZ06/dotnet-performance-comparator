using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

public enum FaseEjecucion
{
    Preparacion,
    Readiness,
    WarmUp,
    RampUp,
    Medicion,
    NivelEstres,
    Concurrencia,
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

/// <summary>Resumen de un <c>NivelEstres</c> finalizado, para la tabla de niveles de M-05 (C-07.3).</summary>
public sealed record NivelEstresResumen(
    string VersionDotNet,
    int NumeroEjecucion,
    int NumeroNivel,
    int UsuariosObjetivo,
    string Estado,
    decimal? Cpu,
    decimal? RamMb,
    decimal? TiempoRespuestaMs,
    decimal? LatenciaP95Ms,
    decimal? ThroughputRps,
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
    EjecucionResumen? EjecucionFinalizada = null,
    int? NivelActual = null,
    int? TotalNiveles = null,
    int? UsuariosNivel = null,
    NivelEstresResumen? NivelFinalizado = null,
    int? RondaActual = null,
    int? TotalRondas = null,
    int? UsuariosConcurrentes = null);

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

    /// <summary>Inserta una <c>Ejecucion</c> con sus condiciones y resultados en un único SaveChanges (Carga).</summary>
    Task AgregarEjecucionAsync(Ejecucion ejecucion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una <c>Ejecucion</c> en estado inicial (con sus condiciones, sin resultados) y devuelve su Id.
    /// Usada por Estrés para persistir niveles incrementalmente bajo una ejecución ya existente (C-07.3).
    /// </summary>
    Task<int> CrearEjecucionEnProcesoAsync(Ejecucion ejecucion, CancellationToken cancellationToken = default);

    /// <summary>Crea un <c>NivelEstres</c> en estado inicial y devuelve su Id.</summary>
    Task<int> AgregarNivelEnProcesoAsync(NivelEstres nivel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finaliza un nivel: actualiza estado/fechaFin/solicitudes/detalle e inserta sus resultados por métrica
    /// (<c>ResultadoMetricaNivelEstres</c>) en un único SaveChanges.
    /// </summary>
    Task CompletarNivelAsync(
        int nivelId, string estado, DateTime fechaFin, long solicitudesExitosas, long solicitudesFallidas,
        string? detalle, IReadOnlyList<(int MetricaId, decimal Valor)> resultados, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finaliza una <c>Ejecucion</c>: actualiza estado/fechaFin y, si se proporciona, inserta el
    /// <c>ResultadoMetrica</c> de resumen (solo cuando la ejecución alcanzó el nivel máximo).
    /// </summary>
    Task FinalizarEjecucionAsync(
        int ejecucionId, string estado, DateTime fechaFin,
        IReadOnlyList<(int MetricaId, decimal Valor)>? resumen, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga la configuración con sus ejecuciones, resultados, condiciones y niveles de estrés, para
    /// reconstruir el estado persistido en M-05 tras recarga/reconexión (C-07.5).
    /// </summary>
    Task<ConfiguracionEvaluacion?> GetConfiguracionConEjecucionesAsync(int configuracionId, CancellationToken cancellationToken = default);
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

/// <summary>Estado persistido de una configuración reconstruido desde la base de datos (C-07.5).</summary>
public sealed record EstadoEjecucionReconstruido(
    ConfiguracionEjecutableInfo Info,
    IReadOnlyList<EjecucionResumen> Ejecuciones,
    IReadOnlyList<NivelEstresResumen> Niveles);

/// <summary>
/// Lectura del estado persistido de una configuración conocida, para reconstruir M-05 tras
/// recarga/reconexión. Exclusivo de CU-04; no es la consulta histórica general de CU-05.
/// </summary>
public interface IExecutionStatusQueryService
{
    Task<EstadoEjecucionReconstruido?> ObtenerEstadoAsync(int configuracionId, CancellationToken cancellationToken = default);
}
