namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>
/// Selecciona el SDK a partir de la salida de <c>dotnet --list-sdks</c>
/// (líneas con el formato <c>10.0.400 [C:\Program Files\dotnet\sdk]</c>).
/// </summary>
public static class DotNetSdkSelector
{
    /// <summary>
    /// Devuelve la mayor versión estable (sin sufijo preview/rc) del major indicado,
    /// o null si no hay ninguna. Las líneas con formato no reconocido se ignoran.
    /// </summary>
    public static string? SelectLatestStable(IEnumerable<string> listSdksOutput, int major)
    {
        Version? mejor = null;
        string? texto = null;

        foreach (var linea in listSdksOutput)
        {
            var token = linea.Trim().Split(' ', 2)[0];
            if (token.Length == 0 || token.Contains('-') || !Version.TryParse(token, out var version))
            {
                continue;
            }

            if (version.Major == major && version.Build >= 0 && (mejor is null || version > mejor))
            {
                mejor = version;
                texto = token;
            }
        }

        return texto;
    }

    /// <summary>Indica si hay alguna versión preliminar del major indicado.</summary>
    public static bool HasPreview(IEnumerable<string> listSdksOutput, int major) =>
        listSdksOutput
            .Select(l => l.Trim().Split(' ', 2)[0])
            .Any(t => t.Contains('-') && Version.TryParse(t.Split('-')[0], out var v) && v.Major == major);
}
