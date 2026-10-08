namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

public enum ProjectFilePickerStatus
{
    /// <summary>El Usuario seleccionó un archivo.</summary>
    Selected,

    /// <summary>El Usuario cerró o canceló el selector (acción normal, no es un error).</summary>
    Cancelled,

    /// <summary>El selector nativo no está disponible en esta plataforma.</summary>
    Unsupported,

    /// <summary>Ya hay un selector abierto.</summary>
    Busy,

    /// <summary>No fue posible abrir el selector.</summary>
    Error
}

public sealed record ProjectFilePickerResult(ProjectFilePickerStatus Status, string? Path = null)
{
    public static ProjectFilePickerResult Selected(string path) => new(ProjectFilePickerStatus.Selected, path);
    public static ProjectFilePickerResult Cancelled { get; } = new(ProjectFilePickerStatus.Cancelled);
    public static ProjectFilePickerResult Unsupported { get; } = new(ProjectFilePickerStatus.Unsupported);
    public static ProjectFilePickerResult Busy { get; } = new(ProjectFilePickerStatus.Busy);
    public static ProjectFilePickerResult Error { get; } = new(ProjectFilePickerStatus.Error);
}

/// <summary>
/// Selector de archivos <c>.csproj</c> del equipo local. Solo obtiene una ruta:
/// no lee, valida, ejecuta ni modifica el archivo (la validación pertenece a CU-01).
/// </summary>
public interface IProjectFilePicker
{
    Task<ProjectFilePickerResult> PickProjectFileAsync(CancellationToken cancellationToken = default);
}
