namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Valores persistidos en <c>Ejecucion.Estado</c>.
/// </summary>
public static class EstadosEjecucion
{
    public const string EnProceso = "EnProceso";
    public const string Completada = "Completada";

    /// <summary>
    /// Estrés (C-07.3): la prueba produjo válidamente un punto de saturación antes o durante el nivel
    /// máximo. No es un fallo técnico; la configuración puede seguir con las demás versiones/repeticiones.
    /// </summary>
    public const string CompletadaConSaturacion = "CompletadaConSaturacion";

    public const string Fallida = "Fallida";
    public const string Cancelada = "Cancelada";
}
