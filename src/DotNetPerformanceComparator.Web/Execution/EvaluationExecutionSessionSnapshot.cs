using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Web.Components.Shared.Charts;

namespace DotNetPerformanceComparator.Web.Execution;

/// <summary>Estado de la sesión de ejecución server-side (independiente del estado persistido).</summary>
public enum EstadoSesion
{
    Preparando,
    Ejecutando,
    Cancelando,
    Finalizada,
    Fallida,
    Cancelada
}

/// <summary>
/// Copia inmutable y segura del estado transitorio de una evaluación activa (o la última finalizada),
/// que M-05 consulta por polling. No contiene referencias mutables internas del manager (C-07.5).
/// </summary>
public sealed record EvaluationExecutionSessionSnapshot(
    int ConfiguracionId,
    EstadoSesion EstadoSesion,
    string? EstadoPersistidoConfiguracion,
    FaseEjecucion? Fase,
    string? VersionActual,
    int? NumeroEjecucionActual,
    int OrdenGlobal,
    int TotalPasos,
    decimal? CpuActual,
    decimal? RamActual,
    int? NivelActual,
    int? TotalNiveles,
    int? UsuariosNivel,
    int? RondaActual,
    int? TotalRondas,
    int? UsuariosConcurrentes,
    string? MensajeActual,
    IReadOnlyList<string> Logs,
    IReadOnlyList<EjecucionResumen> Resultados,
    IReadOnlyList<NivelEstresResumen> Niveles,
    IReadOnlyList<LineChartPoint> SerieCpu,
    DateTime InicioUtc,
    bool CancelacionSolicitada)
{
    public double ProgresoPorcentaje => TotalPasos == 0 ? 0 : Math.Round(OrdenGlobal * 100.0 / TotalPasos);

    public bool Activa => EstadoSesion is EstadoSesion.Preparando or EstadoSesion.Ejecutando or EstadoSesion.Cancelando;

    public bool PuedeCancelar => EstadoSesion is EstadoSesion.Preparando or EstadoSesion.Ejecutando;
}
