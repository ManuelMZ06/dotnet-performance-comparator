namespace DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

/// <summary>
/// Comparación de rutas de directorios al estilo Windows (sin distinguir mayúsculas).
/// </summary>
internal static class PathRules
{
    public static string NormalizeDirectory(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static bool AreSame(string a, string b) =>
        string.Equals(NormalizeDirectory(a), NormalizeDirectory(b), StringComparison.OrdinalIgnoreCase);

    /// <summary>Indica si <paramref name="path"/> es <paramref name="directory"/> o está dentro de él.</summary>
    public static bool IsSameOrInside(string path, string directory)
    {
        var child = NormalizeDirectory(path);
        var parent = NormalizeDirectory(directory);
        if (string.Equals(child, parent, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar;
        return child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
