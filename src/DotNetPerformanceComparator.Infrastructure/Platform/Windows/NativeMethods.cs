using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DotNetPerformanceComparator.Infrastructure.Platform.Windows;

// Interoperabilidad mínima con el diálogo de archivos de Windows Shell (IFileOpenDialog).
// El orden de los métodos de cada interfaz COM debe coincidir exactamente con shobjidl_core.h.

[SupportedOSPlatform("windows")]
internal static class NativeMethods
{
    public const int ErrorCancelled = unchecked((int)0x800704C7); // HRESULT_FROM_WIN32(ERROR_CANCELLED)

    public static readonly Guid FileOpenDialogClsid = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");

    private const uint WsPopup = 0x80000000;
    private const uint WsExToolWindow = 0x00000080;
    private const uint WsExTopmost = 0x00000008;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    // Tamaño inicial habitual del diálogo; Windows lo ubica con su esquina en la ventana propietaria.
    private const int DialogWidth = 960;
    private const int DialogHeight = 600;

    /// <summary>
    /// Ventana invisible y siempre visible encima, usada como propietaria del diálogo para que este
    /// aparezca (aprox.) centrado en la pantalla principal y no quede oculto detrás del navegador
    /// (el proceso servidor no tiene ventana propia).
    /// </summary>
    public static IntPtr CreateTopmostOwnerWindow() =>
        CreateWindowExW(WsExToolWindow | WsExTopmost, "STATIC", null, WsPopup,
            Math.Max(0, (GetSystemMetrics(SmCxScreen) - DialogWidth) / 2),
            Math.Max(0, (GetSystemMetrics(SmCyScreen) - DialogHeight) / 2),
            0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
}

[Flags]
internal enum FileOpenOptions : uint
{
    NoChangeDir = 0x00000008,
    PickFolders = 0x00000020,
    ForceFileSystem = 0x00000040,
    PathMustExist = 0x00000800,
    FileMustExist = 0x00001000
}

internal enum ShellItemDisplayName : uint
{
    FileSystemPath = 0x80058000
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct FilterSpec
{
    [MarshalAs(UnmanagedType.LPWStr)] public string Name;
    [MarshalAs(UnmanagedType.LPWStr)] public string Spec;
}

[ComImport]
[Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    // IModalWindow
    [PreserveSig] int Show(IntPtr parent);

    // IFileDialog
    void SetFileTypes(uint count, [In, MarshalAs(UnmanagedType.LPArray)] FilterSpec[] filters);
    void SetFileTypeIndex(uint index);
    void GetFileTypeIndex(out uint index);
    void Advise(IntPtr events, out uint cookie);
    void Unadvise(uint cookie);
    void SetOptions(FileOpenOptions options);
    void GetOptions(out FileOpenOptions options);
    void SetDefaultFolder(IShellItem item);
    void SetFolder(IShellItem item);
    void GetFolder(out IShellItem item);
    void GetCurrentSelection(out IShellItem item);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
    void GetResult(out IShellItem item);
    void AddPlace(IShellItem item, int placement);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
    void Close(int hr);
    void SetClientGuid(ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr filter);

    // IFileOpenDialog
    void GetResults(out IntPtr items);
    void GetSelectedItems(out IntPtr items);
}

[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid interfaceId, out IntPtr result);
    void GetParent(out IShellItem parent);
    void GetDisplayName(ShellItemDisplayName type, [MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetAttributes(uint mask, out uint attributes);
    void Compare(IShellItem other, uint hint, out int order);
}
