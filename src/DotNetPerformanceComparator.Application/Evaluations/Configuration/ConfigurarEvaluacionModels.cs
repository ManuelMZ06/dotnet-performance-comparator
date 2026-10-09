namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

/// <summary>Parámetros específicos tal como se escriben en M-04 (sin parsear).</summary>
public sealed record ParametrosEvaluacionInput(
    string? WarmUp = null,
    string? UsuariosVirtuales = null,
    string? DuracionSegundos = null,
    string? RampUpSegundos = null,
    string? Intervalo = null,
    string? Observaciones = null);

public sealed record ConfigurarEvaluacionRequest(
    string? Nombre,
    int? ProyectoId,
    int? MigracionId,
    int? NumeroEjecuciones,
    IReadOnlyList<int> TipoPruebaIds,
    IReadOnlyList<int> MetricaIds,
    ParametrosEvaluacionInput Parametros);

public sealed record ConfigurarEvaluacionResult(bool Exito, string Mensaje, int? ConfiguracionId = null)
{
    public static ConfigurarEvaluacionResult Fallo(string mensaje) => new(false, mensaje);
}
