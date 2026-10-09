using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

namespace DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

/// <summary>
/// Copia recursiva del directorio del proyecto hacia un destino nuevo o vacío. Solo lee el origen;
/// excluye bin, obj, .vs y .git en cualquier nivel; no sigue enlaces simbólicos ni sobrescribe archivos.
/// </summary>
public sealed class ProjectCopyService : IProjectCopyService
{
    public DestinationCheck CheckDestination(string sourceDirectory, string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return new DestinationCheck(DestinationStatus.RutaVacia);
        }

        string destino;
        string origen;
        try
        {
            destination = destination.Trim();
            if (!Path.IsPathFullyQualified(destination))
            {
                return new DestinationCheck(DestinationStatus.RutaInvalida);
            }

            destino = PathRules.NormalizeDirectory(destination);
            origen = PathRules.NormalizeDirectory(sourceDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new DestinationCheck(DestinationStatus.RutaInvalida);
        }

        if (PathRules.AreSame(destino, origen))
        {
            return new DestinationCheck(DestinationStatus.IgualAlOrigen, destino);
        }

        if (PathRules.IsSameOrInside(destino, origen))
        {
            return new DestinationCheck(DestinationStatus.DentroDelOrigen, destino);
        }

        if (PathRules.IsSameOrInside(origen, destino))
        {
            return new DestinationCheck(DestinationStatus.ContieneAlOrigen, destino);
        }

        try
        {
            if (File.Exists(destino))
            {
                return new DestinationCheck(DestinationStatus.EsArchivo, destino);
            }

            if (Directory.Exists(destino))
            {
                return Directory.EnumerateFileSystemEntries(destino).Any()
                    ? new DestinationCheck(DestinationStatus.NoVacio, destino)
                    : new DestinationCheck(DestinationStatus.Valido, destino, Existed: true);
            }

            var padre = Path.GetDirectoryName(destino);
            return padre is not null && Directory.Exists(padre)
                ? new DestinationCheck(DestinationStatus.Valido, destino, Existed: false)
                : new DestinationCheck(DestinationStatus.PadreNoExiste, destino);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return new DestinationCheck(DestinationStatus.RutaInvalida, destino);
        }
    }

    public Task<CopyResult> CopyAsync(string sourceDirectory, DestinationCheck destination, CancellationToken cancellationToken = default)
    {
        if (!destination.IsValid || destination.FullPath is null)
        {
            return Task.FromResult(new CopyResult(false, 0, [], "El directorio de salida no es válido."));
        }

        // Evita bloquear el hilo del circuito de Blazor durante la copia.
        return Task.Run(() => Copy(sourceDirectory, destination, cancellationToken), CancellationToken.None);
    }

    private static CopyResult Copy(string sourceDirectory, DestinationCheck destination, CancellationToken cancellationToken)
    {
        var destino = destination.FullPath!;
        var omitidos = new List<string>();
        var copiados = 0;

        try
        {
            var origen = new DirectoryInfo(PathRules.NormalizeDirectory(sourceDirectory));
            Directory.CreateDirectory(destino);
            CopyDirectory(origen, origen, destino, omitidos, ref copiados, cancellationToken);
            return new CopyResult(true, copiados, omitidos);
        }
        catch (Exception ex)
        {
            var mensaje = ex switch
            {
                OperationCanceledException => "La copia fue cancelada.",
                UnauthorizedAccessException => "Se denegó el acceso a un archivo o directorio durante la copia.",
                IOException => "No fue posible copiar un archivo o directorio (puede estar en uso o no haber espacio suficiente).",
                _ => "Ocurrió un error inesperado durante la copia."
            };

            if (!Limpiar(destino, eliminarDirectorio: !destination.Existed))
            {
                mensaje += " No fue posible eliminar por completo la copia incompleta.";
            }

            return new CopyResult(false, copiados, omitidos, mensaje);
        }
    }

    private static void CopyDirectory(
        DirectoryInfo raiz,
        DirectoryInfo directorio,
        string destino,
        List<string> omitidos,
        ref int copiados,
        CancellationToken cancellationToken)
    {
        foreach (var entrada in directorio.EnumerateFileSystemInfos())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativa = Path.GetRelativePath(raiz.FullName, entrada.FullName);

            if (entrada.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                omitidos.Add(relativa);
                continue;
            }

            var objetivo = Path.Combine(destino, entrada.Name);
            if (entrada is DirectoryInfo subdirectorio)
            {
                if (ProjectCopyRules.ExcludedDirectories.Contains(subdirectorio.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                Directory.CreateDirectory(objetivo);
                CopyDirectory(raiz, subdirectorio, objetivo, omitidos, ref copiados, cancellationToken);
            }
            else if (entrada is FileInfo archivo)
            {
                archivo.CopyTo(objetivo, overwrite: false);
                copiados++;
            }
        }
    }

    /// <summary>
    /// Elimina lo creado por la copia. Si el directorio existía (vacío) antes, solo se elimina su contenido.
    /// </summary>
    private static bool Limpiar(string destino, bool eliminarDirectorio)
    {
        try
        {
            if (!Directory.Exists(destino))
            {
                return true;
            }

            if (eliminarDirectorio)
            {
                Directory.Delete(destino, recursive: true);
                return true;
            }

            var directorio = new DirectoryInfo(destino);
            foreach (var archivo in directorio.EnumerateFiles())
            {
                archivo.Delete();
            }

            foreach (var sub in directorio.EnumerateDirectories())
            {
                sub.Delete(recursive: true);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
