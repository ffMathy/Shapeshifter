using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Shapeshifter.Platform.Windows;

/// <summary>Desktop Window Manager settings for windows that use the system's Acrylic or Mica backdrop.</summary>
[SupportedOSPlatform("windows")]
public static partial class WindowsWindowMaterial
{
    private const int DwmwaBorderColor = 34;
    private const uint DwmwaColorNone = 0xFFFFFFFE;

    /// <summary>
    /// Whether this Windows rounds the corners of windows and gives them a backdrop and shadow: Windows 11 (build 22000)
    /// and later.
    /// </summary>
    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    /// <summary>
    /// Removes the 1 px border that Windows 11 draws around a window, for windows that draw their own stroke on top of
    /// the backdrop. Returns false when it could not be removed.
    /// </summary>
    public static bool HideBorder(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsSupported)
        {
            return false;
        }

        var none = DwmwaColorNone;
        return DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref none, sizeof(uint)) == 0;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint value, int size);
}
