using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Shapeshifter.Platform.Windows;

/// <summary>Desktop Window Manager settings for windows that use the system's Acrylic or Mica backdrop.</summary>
[SupportedOSPlatform("windows")]
public static partial class WindowsWindowMaterial
{
    private const int DwmwaBorderColor = 34;
    private const int DwmwaSystemBackdropType = 38;
    private const uint DwmwaColorNone = 0xFFFFFFFE;
    private const int DwmsbtMainWindow = 2;

    /// <summary>
    /// Whether this Windows rounds the corners of windows and gives them a backdrop and shadow: Windows 11 (build 22000)
    /// and later.
    /// </summary>
    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    /// <summary>
    /// Whether DWM draws the system backdrops itself (DWMWA_SYSTEMBACKDROP_TYPE): Windows 11 2022 Update (build 22621)
    /// and later.
    /// </summary>
    public static bool IsSystemBackdropSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);

    /// <summary>
    /// Whether "Transparency effects" is on (Settings, Personalisation, Colours). When it is off, Windows draws its own
    /// backdrops as solid colours; Avalonia's Acrylic and Mica do not know about it.
    /// </summary>
    public static bool AreTransparencyEffectsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("EnableTransparency") is not int value || value != 0;
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return true;
            }
        }
    }

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

    /// <summary>
    /// Gives a decorated window the system's Mica, drawn by DWM as in the Settings app: behind the title bar (keeping the
    /// native caption and its buttons) and, with the frame extended over the whole window, behind the client area, which
    /// has to be transparent. The tint follows the window's dark-mode attribute. Returns false, and leaves the window as it
    /// was, where Windows has no system backdrops. Idempotent, so it can be called again whenever the frame may have been
    /// reset.
    /// </summary>
    /// <remarks>
    /// The frame margins of -1 must stay: anything that calls DwmExtendFrameIntoClientArea again for the window (in
    /// Avalonia 12.1, WindowImpl.UpdateWindowProperties on a change of decorations or a forced change, as on leaving full
    /// screen or showing a window whose decorations are not Full, and WindowImpl.ExtendClientArea) takes the Mica out of
    /// the client area, which then shows black or unblurred. Call this again after such a change.
    /// </remarks>
    public static bool TryApplyMainWindowMica(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsSystemBackdropSupported)
        {
            return false;
        }

        var backdrop = DwmsbtMainWindow;
        if (DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdrop, sizeof(int)) != 0)
        {
            return false;
        }

        var sheet = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        if (DwmExtendFrameIntoClientArea(hwnd, ref sheet) == 0)
        {
            return true;
        }

        RemoveMainWindowMica(hwnd);
        return false;
    }

    /// <summary>
    /// Undoes <see cref="TryApplyMainWindowMica"/>: no system backdrop and no frame over the client area, as Avalonia
    /// leaves a decorated window.
    /// </summary>
    public static void RemoveMainWindowMica(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsSystemBackdropSupported)
        {
            return;
        }

        var none = 0;
        DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref none, sizeof(int));
        var margins = default(Margins);
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint value, int size);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
}
