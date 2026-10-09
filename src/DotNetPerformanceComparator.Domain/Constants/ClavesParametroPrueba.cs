namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Claves persistidas en <c>ParametroEvaluacion.Clave</c>, agrupadas por tipo de prueba.
/// Identificadores internos estables, sin espacios ni acentos.
/// </summary>
public static class ClavesParametroCarga
{
    public const string UsuariosVirtuales = "UsuariosVirtuales";
    public const string DuracionSegundos = "DuracionSegundos";
    public const string WarmUpSegundos = "WarmUpSegundos";
    public const string RampUpSegundos = "RampUpSegundos";
    public const string IntervaloSolicitudesMs = "IntervaloSolicitudesMs";
}

public static class ClavesParametroEstres
{
    public const string UsuariosIniciales = "UsuariosIniciales";
    public const string IncrementoUsuarios = "IncrementoUsuarios";
    public const string UsuariosMaximos = "UsuariosMaximos";
    public const string DuracionNivelSegundos = "DuracionNivelSegundos";
    public const string WarmUpSegundos = "WarmUpSegundos";
    public const string IntervaloSolicitudesMs = "IntervaloSolicitudesMs";
}

public static class ClavesParametroConcurrencia
{
    public const string UsuariosConcurrentes = "UsuariosConcurrentes";
    public const string Rondas = "Rondas";
    public const string WarmUpSegundos = "WarmUpSegundos";
    public const string IntervaloRondasMs = "IntervaloRondasMs";
}
