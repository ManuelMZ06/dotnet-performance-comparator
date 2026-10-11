namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Determina el ensamblado de entrada de una carpeta de publish a partir del <c>*.runtimeconfig.json</c>
/// de la raíz (no asume <c>nombre-csproj.dll</c>, porque AssemblyName puede diferir).
/// </summary>
internal static class EntryAssemblyResolver
{
    public static bool TryResolve(string publishDirectory, out string entryAssemblyPath, out string? error)
    {
        entryAssemblyPath = string.Empty;
        error = null;

        if (!Directory.Exists(publishDirectory))
        {
            error = "No se encontró la carpeta de publicación.";
            return false;
        }

        var candidatos = Directory.EnumerateFiles(publishDirectory, "*.runtimeconfig.json", SearchOption.TopDirectoryOnly)
            .Select(rc => Path.ChangeExtension(rc, null)) // quita .json → queda "<app>.runtimeconfig"
            .Select(rc => Path.ChangeExtension(rc, null)) // quita .runtimeconfig → queda "<app>"
            .Select(baseName => baseName + ".dll")
            .Where(File.Exists)
            .ToList();

        if (candidatos.Count == 1)
        {
            entryAssemblyPath = candidatos[0];
            return true;
        }

        error = candidatos.Count == 0
            ? "No fue posible determinar el ensamblado de entrada de la aplicación publicada."
            : "La publicación contiene varios ensamblados de entrada; no se puede determinar inequívocamente cuál ejecutar.";
        return false;
    }
}
