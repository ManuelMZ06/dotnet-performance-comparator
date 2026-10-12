namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Claves persistidas en <c>CondicionEjecucion.Clave</c> (contexto reproducible de una ejecución).
/// </summary>
public static class ClavesCondicionEjecucion
{
    public const string TipoPrueba = "TipoPrueba";
    public const string MetodoHttp = "MetodoHttp";
    public const string RutaObjetivo = "RutaObjetivo";
    public const string ConfiguracionCompilacion = "ConfiguracionCompilacion";
    public const string TargetFramework = "TargetFramework";
    public const string SdkVersion = "SdkVersion";
    public const string Environment = "Environment";
    public const string IntervaloMuestreoMs = "IntervaloMuestreoMs";
    public const string OrdenGlobal = "OrdenGlobal";
    public const string NumeroNivelesPlanificados = "NumeroNivelesPlanificados";
    public const string SolicitudesExitosas = "SolicitudesExitosas";
    public const string SolicitudesFallidas = "SolicitudesFallidas";

    /// <summary>
    /// Entorno con el que se lanza la aplicación objetivo (DOTNET_ENVIRONMENT / ASPNETCORE_ENVIRONMENT),
    /// forzado explícitamente para no heredar el del evaluador. Fuente única para el launcher y la condición.
    /// </summary>
    public const string EntornoObjetivo = "Production";
}

/// <summary>
/// Nombres de métricas del catálogo soportadas por el motor en C-07.1.
/// </summary>
public static class NombresMetrica
{
    public const string Cpu = "CPU";
    public const string MemoriaRam = "Memoria RAM";
    public const string TiempoRespuesta = "Tiempo de respuesta";
    public const string Latencia = "Latencia";
    public const string Throughput = "Throughput";
    public const string TamanoAplicacion = "Tamaño de la aplicación";
}
