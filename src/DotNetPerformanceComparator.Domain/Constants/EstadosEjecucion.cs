namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Valores persistidos en <c>Ejecucion.Estado</c>.
/// </summary>
public static class EstadosEjecucion
{
    public const string EnProceso = "EnProceso";
    public const string Completada = "Completada";
    public const string Fallida = "Fallida";
    public const string Cancelada = "Cancelada";
}
