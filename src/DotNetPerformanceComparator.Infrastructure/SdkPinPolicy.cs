namespace DotNetPerformanceComparator.Infrastructure;

/// <summary>
/// Política única con la que la herramienta fija un SDK de .NET concreto: se usa tanto en el
/// <c>global.json</c> temporal del ejecutor de comandos como en el <c>global.json</c> sincronizado de la copia
/// experimental (BUG-03). Mantiene una sola fuente de verdad para <c>rollForward</c> y <c>allowPrerelease</c>.
/// </summary>
internal static class SdkPinPolicy
{
    /// <summary>Fija exactamente la versión indicada (sin avanzar a otra), para reproducibilidad.</summary>
    public const string RollForward = "disable";

    public const bool AllowPrerelease = false;
}
