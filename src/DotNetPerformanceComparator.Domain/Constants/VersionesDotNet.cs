namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Versiones de .NET dentro del alcance del proyecto, tal como se persisten
/// (p. ej. en <c>ProyectoDotNet.VersionBase</c> y <c>Migracion.VersionOrigen/VersionDestino</c>).
/// </summary>
public static class VersionesDotNet
{
    public const string Net8 = ".NET 8";

    /// <summary>Target Framework Moniker de .NET 8.</summary>
    public const string Net8Tfm = "net8.0";

    public const string Net10 = ".NET 10";

    /// <summary>Target Framework Moniker de .NET 10.</summary>
    public const string Net10Tfm = "net10.0";
}
