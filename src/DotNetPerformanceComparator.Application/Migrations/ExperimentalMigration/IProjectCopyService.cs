namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

public enum DestinationStatus
{
    Valido,
    RutaVacia,
    RutaInvalida,
    IgualAlOrigen,
    DentroDelOrigen,
    ContieneAlOrigen,
    EsArchivo,
    NoVacio,
    PadreNoExiste
}

/// <param name="FullPath">Ruta absoluta normalizada, si la ruta era válida.</param>
/// <param name="Existed">El directorio ya existía (vacío) antes de la migración.</param>
public sealed record DestinationCheck(DestinationStatus Status, string? FullPath = null, bool Existed = false)
{
    public bool IsValid => Status == DestinationStatus.Valido;
}

/// <param name="Success">Se copiaron todos los archivos.</param>
/// <param name="FilesCopied">Cantidad de archivos copiados.</param>
/// <param name="SkippedLinks">Enlaces simbólicos/uniones omitidos (no se siguen).</param>
/// <param name="ErrorMessage">Causa comprensible si falló.</param>
public sealed record CopyResult(bool Success, int FilesCopied, IReadOnlyList<string> SkippedLinks, string? ErrorMessage = null);

public static class ProjectCopyRules
{
    /// <summary>Directorios excluidos de la copia (artefactos regenerables), en cualquier nivel.</summary>
    public static IReadOnlyList<string> ExcludedDirectories { get; } = ["bin", "obj", ".vs", ".git"];
}

/// <summary>
/// Copia el directorio de un proyecto hacia un destino nuevo o vacío, sin modificar el origen.
/// </summary>
public interface IProjectCopyService
{
    DestinationCheck CheckDestination(string sourceDirectory, string? destination);

    /// <summary>
    /// Copia <paramref name="sourceDirectory"/> en <paramref name="destination"/>. Si falla, elimina
    /// únicamente lo creado por esta operación (nunca un directorio que existía antes).
    /// </summary>
    Task<CopyResult> CopyAsync(string sourceDirectory, DestinationCheck destination, CancellationToken cancellationToken = default);
}
