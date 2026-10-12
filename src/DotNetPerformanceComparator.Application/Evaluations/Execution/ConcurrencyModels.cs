namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>Parámetros de una prueba de Concurrencia por rondas sincronizadas (C-07.4).</summary>
public sealed record ParametrosConcurrencia(int UsuariosConcurrentes, int Rondas, int WarmUpSegundos, int IntervaloRondasMs);

/// <summary>
/// Solicitud para la fase oficial de Concurrencia. Las callbacks de progreso son delegados simples (no
/// exponen NBomber): se invocan al liberar y al completar cada ronda.
/// </summary>
public sealed record ConcurrencyRunRequest(
    Uri BaseUrl,
    string RutaObjetivo,
    int UsuariosConcurrentes,
    int Rondas,
    int IntervaloRondasMs,
    Action<int>? OnRondaLiberada = null,
    Action<int, double>? OnRondaCompletada = null);

/// <summary>
/// Resultado de la fase oficial de Concurrencia (todas las rondas en una sola ejecución NBomber). Las
/// estadísticas HTTP corresponden al conjunto de todas las rondas (P95 global, no promedio de percentiles).
/// </summary>
/// <param name="Started">El motor pudo iniciarse.</param>
/// <param name="SolicitudesEsperadas">N·Rondas.</param>
/// <param name="TiempoRespuestaMedioMs">Mean de latencia OK del Step "request".</param>
/// <param name="LatenciaP95Ms">P95 de latencia OK del Step "request".</param>
/// <param name="ThroughputRps">RPS OK del Step "request" (incluye los intervalos entre rondas).</param>
/// <param name="DesfaseInicioMaximoMs">Desfase de inicio máximo entre rondas (diagnóstico).</param>
/// <param name="DesfaseInicioPromedioMs">Desfase de inicio promedio entre rondas (diagnóstico).</param>
/// <param name="TimeoutSincronizacion">true si una barrera agotó su timeout técnico.</param>
public sealed record ConcurrencyPhaseResult(
    bool Started,
    long SolicitudesEsperadas,
    long SolicitudesExitosas,
    long SolicitudesFallidas,
    double TiempoRespuestaMedioMs,
    double LatenciaP95Ms,
    double ThroughputRps,
    double DesfaseInicioMaximoMs,
    double DesfaseInicioPromedioMs,
    bool TimeoutSincronizacion);

/// <summary>
/// Ejecuta la fase oficial de Concurrencia (rondas sincronizadas) sobre un proceso objetivo ya listo.
/// Implementado en Infrastructure (NBomber + barrera); Application solo recibe <see cref="ConcurrencyPhaseResult"/>.
/// </summary>
public interface IConcurrencyRunner
{
    Task<ConcurrencyPhaseResult> RunAsync(ConcurrencyRunRequest request, CancellationToken cancellationToken = default);
}
