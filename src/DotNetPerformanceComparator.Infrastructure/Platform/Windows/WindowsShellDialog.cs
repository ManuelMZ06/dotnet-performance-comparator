using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Infrastructure.Platform.Windows;

internal enum ShellDialogOutcome
{
    Selected,
    Cancelled,
    Unsupported,
    Busy,
    Error
}

/// <summary>
/// Muestra el diálogo nativo Windows Shell <c>IFileOpenDialog</c> en un hilo STA dedicado que termina
/// al cerrarse. Solo puede haber un diálogo abierto a la vez en toda la herramienta. El diálogo tiene
/// como propietaria una ventana oculta "siempre encima" para que aparezca al frente.
/// </summary>
internal static class WindowsShellDialog
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static Task<(ShellDialogOutcome Outcome, string? Path)> ShowAsync(
        Action<IFileOpenDialog> configure,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<(ShellDialogOutcome, string?)>((ShellDialogOutcome.Unsupported, null));
        }

        if (!Gate.Wait(0))
        {
            return Task.FromResult<(ShellDialogOutcome, string?)>((ShellDialogOutcome.Busy, null));
        }

        var completion = new TaskCompletionSource<(ShellDialogOutcome, string?)>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(OperatingSystem.IsWindows()
                        ? Show(configure)
                        : (ShellDialogOutcome.Unsupported, null));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "No fue posible abrir el diálogo de Windows.");
                    completion.SetResult((ShellDialogOutcome.Error, null));
                }
                finally
                {
                    Gate.Release();
                }
            })
            {
                IsBackground = true,
                Name = "Diálogo de Windows"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch (Exception ex)
        {
            Gate.Release();
            logger.LogError(ex, "No fue posible iniciar el hilo del diálogo de Windows.");
            return Task.FromResult<(ShellDialogOutcome, string?)>((ShellDialogOutcome.Error, null));
        }

        return completion.Task;
    }

    [SupportedOSPlatform("windows")]
    private static (ShellDialogOutcome, string?) Show(Action<IFileOpenDialog> configure)
    {
        var dialog = (IFileOpenDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(NativeMethods.FileOpenDialogClsid, throwOnError: true)!)!;
        var owner = NativeMethods.CreateTopmostOwnerWindow();
        IShellItem? item = null;
        try
        {
            configure(dialog);

            var hr = dialog.Show(owner);
            if (hr == NativeMethods.ErrorCancelled)
            {
                return (ShellDialogOutcome.Cancelled, null);
            }

            Marshal.ThrowExceptionForHR(hr);

            dialog.GetResult(out item);
            item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out var path);
            return (ShellDialogOutcome.Selected, path);
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
