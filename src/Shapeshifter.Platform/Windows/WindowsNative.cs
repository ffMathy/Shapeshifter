using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Windows;

[SupportedOSPlatform("windows")]
internal static partial class WindowsNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    /// <summary>Incremented by Windows on every clipboard change; reading it does not open the clipboard.</summary>
    [LibraryImport("user32.dll")]
    public static partial uint GetClipboardSequenceNumber();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out Point point);

    // ---- Clipboard source: windows, processes and icons ----

    public const uint ProcessQueryLimitedInformation = 0x1000;
    public const uint WmGetIcon = 0x007F;
    public const nint IconBig = 1;
    public const uint SmtoAbortIfHung = 0x0002;
    public const int GclpHIcon = -14;
    public const uint GaRoot = 2;
    public const uint DibRgbColors = 0;
    public const uint BiRgb = 0;

    public const uint ShgfiIcon = 0x000000100;
    public const uint ShgfiSysIconIndex = 0x000004000;
    public const uint ShgfiLargeIcon = 0x000000000;
    public const uint ShgfiSmallIcon = 0x000000001;
    public const uint ShgfiUseFileAttributes = 0x000000010;
    public const uint FileAttributeDirectory = 0x00000010;
    public const uint FileAttributeNormal = 0x00000080;

    public const int ShilLarge = 0;
    public const int ShilSmall = 1;
    public const int ShilExtraLarge = 2;
    public const int ShilJumbo = 4;
    public const uint IldTransparent = 0x00000001;

    /// <summary>IID_IImageList.</summary>
    public static readonly Guid ImageListInterfaceId = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

    [StructLayout(LayoutKind.Sequential)]
    public struct IconInfo
    {
        public int IsIcon;
        public uint HotspotX;
        public uint HotspotY;
        public nint MaskBitmap;
        public nint ColorBitmap;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bitmap
    {
        public int Type;
        public int Width;
        public int Height;
        public int WidthBytes;
        public ushort Planes;
        public ushort BitsPixel;
        public nint Bits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct ShFileInfo
    {
        public nint Icon;
        public int IconIndex;
        public uint Attributes;
        public fixed char DisplayName[260];
        public fixed char TypeName[80];
    }

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    // ---- Moving the keyboard focus between the picker and the window the user works in ----

    public const int SwRestore = 9;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BringWindowToTop(nint window);

    [LibraryImport("user32.dll")]
    public static partial nint SetFocus(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint window, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(uint attach, uint attachTo, [MarshalAs(UnmanagedType.Bool)] bool doAttach);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetClipboardOwner();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(nint window);

    [LibraryImport("user32.dll")]
    public static partial nint GetAncestor(nint window, uint flags);

    [LibraryImport("user32.dll")]
    public static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    public static partial int GetWindowTextLength(nint window);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
    public static unsafe partial int GetWindowText(nint window, char* text, int maxCount);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool EnumChildWindows(nint parent, delegate* unmanaged<nint, nint, int> callback, nint state);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    public static partial nint SendMessageTimeout(
        nint window, uint message, nint wParam, nint lParam, uint flags, uint timeoutMilliseconds, out nint result);

    [LibraryImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
    private static partial nint GetClassLongPtr64(nint window, int index);

    [LibraryImport("user32.dll", EntryPoint = "GetClassLongW")]
    private static partial uint GetClassLong32(nint window, int index);

    /// <summary>GetClassLongPtrW only exists in 64-bit user32; 32-bit Windows exports GetClassLongW instead.</summary>
    public static nint GetClassLongPtr(nint window, int index) =>
        IntPtr.Size == 8 ? GetClassLongPtr64(window, index) : (nint)GetClassLong32(window, index);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetIconInfo(nint icon, out IconInfo info);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(nint icon);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool QueryFullProcessImageName(nint process, uint flags, char* name, ref uint size);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateCompatibleDC(nint dc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint dc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint gdiObject);

    [LibraryImport("gdi32.dll", EntryPoint = "GetObjectW")]
    public static partial int GetObject(nint gdiObject, int size, out Bitmap bitmap);

    [LibraryImport("gdi32.dll")]
    public static unsafe partial int GetDIBits(
        nint dc, nint bitmap, uint startScan, uint scanLines, void* bits, void* bitmapInfo, uint usage);

    [LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nuint SHGetFileInfo(string path, uint fileAttributes, ref ShFileInfo info, uint infoSize, uint flags);

    [LibraryImport("shell32.dll", EntryPoint = "SHDefExtractIconW", StringMarshalling = StringMarshalling.Utf16)]
    public static unsafe partial int SHDefExtractIcon(
        string iconFile, int index, uint flags, nint* largeIcon, nint* smallIcon, uint iconSizes);

    /// <summary>Returns the system image list of the given size as an IImageList COM pointer (release it).</summary>
    [LibraryImport("shell32.dll", EntryPoint = "SHGetImageList")]
    public static partial int SHGetImageList(int imageList, in Guid interfaceId, out nint imageListObject);
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsCursorLocator : ICursorLocator
{
    public (int X, int Y)? GetCursorPosition() =>
        WindowsNative.GetCursorPos(out var point) ? (point.X, point.Y) : null;
}
