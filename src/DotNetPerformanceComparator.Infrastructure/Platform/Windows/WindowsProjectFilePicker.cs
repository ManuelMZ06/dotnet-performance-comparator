using DotNetPerformanceComparator.Application.Projects.Incorporation;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Platform.Windows;

/// <summary>
/// Selector nativo de archivos <c>.csproj</c> de Windows (Shell <c>IFileOpenDialog</c>).
/// Únicamente devuelve la ruta seleccionada.
/// </summary>
public sealed class WindowsProjectFilePicker : IProjectFilePicker
{
    private readonly ILogger<WindowsProjectFilePicker> _logger;

    public WindowsProjectFilePicker(ILogger<WindowsProjectFilePicker> logger)
    {
        _logger = logger;
    }

    public async Task<ProjectFilePickerResult> PickProjectFileAsync(CancellationToken cancellationToken = default)
    {
        var (outcome, path) = await WindowsShellDialog.ShowAsync(Configure, _logger, cancellationToken);
        return outcome switch
        {
            ShellDialogOutcome.Selected => ProjectFilePickerResult.Selected(path!),
            ShellDialogOutcome.Cancelled => ProjectFilePickerResult.Cancelled,
            ShellDialogOutcome.Unsupported => ProjectFilePickerResult.Unsupported,
            ShellDialogOutcome.Busy => ProjectFilePickerResult.Busy,
            _ => ProjectFilePickerResult.Error
        };
    }

    private static void Configure(IFileOpenDialog dialog)
    {
        dialog.GetOptions(out var options);
        dialog.SetOptions(options | FileOpenOptions.ForceFileSystem | FileOpenOptions.FileMustExist
            | FileOpenOptions.PathMustExist | FileOpenOptions.NoChangeDir);
        dialog.SetFileTypes(1, [new FilterSpec { Name = "Proyecto .NET (*.csproj)", Spec = "*.csproj" }]);
        dialog.SetFileTypeIndex(1);
        dialog.SetTitle("Seleccionar proyecto .NET 8 (.csproj)");
        dialog.SetOkButtonLabel("Seleccionar");
    }
}
