namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

public enum DirectoryPickerStatus
{
    Selected,
    Cancelled,
    Unsupported,
    Busy,
    Error
}

public sealed record DirectoryPickerResult(DirectoryPickerStatus Status, string? Path = null)
{
    public static DirectoryPickerResult Selected(string path) => new(DirectoryPickerStatus.Selected, path);
    public static DirectoryPickerResult Cancelled { get; } = new(DirectoryPickerStatus.Cancelled);
    public static DirectoryPickerResult Unsupported { get; } = new(DirectoryPickerStatus.Unsupported);
    public static DirectoryPickerResult Busy { get; } = new(DirectoryPickerStatus.Busy);
    public static DirectoryPickerResult Error { get; } = new(DirectoryPickerStatus.Error);
}

/// <summary>
/// Selector de carpetas del equipo local (directorio de salida de CU-02). Solo obtiene una ruta.
/// </summary>
public interface IDirectoryPicker
{
    Task<DirectoryPickerResult> PickDirectoryAsync(CancellationToken cancellationToken = default);
}
