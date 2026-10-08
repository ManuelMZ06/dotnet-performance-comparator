using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Platform.Windows;

/// <summary>
/// Selector nativo de archivos <c>.csproj</c> de Windows (Shell <c>IFileOpenDialog</c>).
/// El diálogo se muestra en un hilo STA dedicado que termina al cerrarse; solo puede
/// haber un diálogo abierto a la vez. Únicamente devuelve la ruta seleccionada.
/// </summary>
public sealed class WindowsProjectFilePicker : IProjectFilePicker
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<WindowsProjectFilePicker> _logger;

    public WindowsProjectFilePicker(ILogger<WindowsProjectFilePicker> logger)
    {
        _logger = logger;
    }

    public Task<ProjectFilePickerResult> PickProjectFileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(ProjectFilePickerResult.Unsupported);
        }

        if (!_gate.Wait(0))
        {
            return Task.FromResult(ProjectFilePickerResult.Busy);
        }

        var completion = new TaskCompletionSource<ProjectFilePickerResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(OperatingSystem.IsWindows() ? ShowDialog() : ProjectFilePickerResult.Unsupported);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No fue posible abrir el selector de archivos de Windows.");
                    completion.SetResult(ProjectFilePickerResult.Error);
                }
                finally
                {
                    _gate.Release();
                }
            })
            {
                IsBackground = true,
                Name = "Selector de proyecto .csproj"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch (Exception ex)
        {
            _gate.Release();
            _logger.LogError(ex, "No fue posible iniciar el hilo del selector de archivos.");
            return Task.FromResult(ProjectFilePickerResult.Error);
        }

        return completion.Task;
    }

    [SupportedOSPlatform("windows")]
    private static ProjectFilePickerResult ShowDialog()
    {
        var dialog = (IFileOpenDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(NativeMethods.FileOpenDialogClsid, throwOnError: true)!)!;
        var owner = NativeMethods.CreateTopmostOwnerWindow();
        IShellItem? item = null;
        try
        {
            dialog.GetOptions(out var options);
            dialog.SetOptions(options | FileOpenOptions.ForceFileSystem | FileOpenOptions.FileMustExist
                | FileOpenOptions.PathMustExist | FileOpenOptions.NoChangeDir);
            dialog.SetFileTypes(1, [new FilterSpec { Name = "Proyecto .NET (*.csproj)", Spec = "*.csproj" }]);
            dialog.SetFileTypeIndex(1);
            dialog.SetTitle("Seleccionar proyecto .NET 8 (.csproj)");
            dialog.SetOkButtonLabel("Seleccionar");

            var hr = dialog.Show(owner);
            if (hr == NativeMethods.ErrorCancelled)
            {
                return ProjectFilePickerResult.Cancelled;
            }

            Marshal.ThrowExceptionForHR(hr);

            dialog.GetResult(out item);
            item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out var path);
            return ProjectFilePickerResult.Selected(path);
        }
        finally
        {
            if (item is not null)
            {
                Marshal.FinalReleaseComObject(item);
            }

            Marshal.FinalReleaseComObject(dialog);

            if (owner != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(owner);
            }
        }
    }
}
