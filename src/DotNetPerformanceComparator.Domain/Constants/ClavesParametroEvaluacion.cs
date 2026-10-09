namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Claves persistidas en <c>ParametroEvaluacion.Clave</c>. Identificadores internos estables,
/// sin espacios ni acentos; el texto mostrado al Usuario se define en la capa de presentación.
/// </summary>
public static class ClavesParametroEvaluacion
{
    public const string WarmUp = "WarmUp";
    public const string UsuariosVirtuales = "UsuariosVirtuales";
    public const string DuracionSegundos = "DuracionSegundos";
    public const string RampUpSegundos = "RampUpSegundos";
    public const string Intervalo = "Intervalo";
    public const string Observaciones = "Observaciones";
}
