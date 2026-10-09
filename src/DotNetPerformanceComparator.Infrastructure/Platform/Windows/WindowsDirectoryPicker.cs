using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Platform.Windows;

/// <summary>
/// Selector nativo de carpetas de Windows (Shell <c>IFileOpenDialog</c> con <c>FOS_PICKFOLDERS</c>).
/// Únicamente devuelve la ruta seleccionada; no crea ni modifica carpetas.
/// </summary>
public sealed class WindowsDirectoryPicker : IDirectoryPicker
{
    private readonly ILogger<WindowsDirectoryPicker> _logger;

    public WindowsDirectoryPicker(ILogger<WindowsDirectoryPicker> logger)
    {
        _logger = logger;
    }

    public async Task<DirectoryPickerResult> PickDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var (outcome, path) = await WindowsShellDialog.ShowAsync(Configure, _logger, cancellationToken);
        return outcome switch
        {
            ShellDialogOutcome.Selected => DirectoryPickerResult.Selected(path!),
            ShellDialogOutcome.Cancelled => DirectoryPickerResult.Cancelled,
            ShellDialogOutcome.Unsupported => DirectoryPickerResult.Unsupported,
            ShellDialogOutcome.Busy => DirectoryPickerResult.Busy,
            _ => DirectoryPickerResult.Error
        };
    }

    private static void Configure(IFileOpenDialog dialog)
    {
        dialog.GetOptions(out var options);
        dialog.SetOptions(options | FileOpenOptions.PickFolders | FileOpenOptions.ForceFileSystem
            | FileOpenOptions.PathMustExist | FileOpenOptions.NoChangeDir);
        dialog.SetTitle("Seleccionar directorio de salida de la copia experimental");
        dialog.SetOkButtonLabel("Seleccionar carpeta");
    }
}
