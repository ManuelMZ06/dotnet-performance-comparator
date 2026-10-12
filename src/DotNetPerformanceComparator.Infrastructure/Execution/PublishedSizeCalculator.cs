namespace DotNetPerformanceComparator.Infrastructure.Execution;

/// <summary>
/// Calcula el "tamaño total del contenido publicado": suma recursiva, en bytes, de todos los archivos
/// regulares bajo un directorio de publish. Se usa una sola vez por versión, inmediatamente después de
/// un <c>dotnet publish</c> correcto y antes de iniciar cualquier proceso objetivo (C-07.2), de modo que
/// los archivos de runtime (logs, SQLite, cachés) no alteren la medición.
/// </summary>
public static class PublishedSizeCalculator
{
    public const long BytesPorMb = 1024L * 1024L;

    /// <summary>Suma recursiva del tamaño de todos los archivos del directorio; 0 si no existe o está vacío.</summary>
    public static long CalculateBytes(string directorio)
    {
        if (string.IsNullOrWhiteSpace(directorio) || !Directory.Exists(directorio))
        {
            return 0;
        }

        long total = 0;
        foreach (var archivo in Directory.EnumerateFiles(directorio, "*", SearchOption.AllDirectories))
        {
            total += new FileInfo(archivo).Length;
        }

        return total;
    }

    /// <summary>Convierte bytes a MB (bytes / 1024²) como <see cref="decimal"/>, sin redondeo.</summary>
    public static decimal BytesToMb(long bytes) => (decimal)bytes / BytesPorMb;
}
