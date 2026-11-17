using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Platform.MacOS;
using Shapeshifter.Platform.Windows;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Gives the picker and the copy notification the operating system's own frosted material, clipped to their rounded
/// panel and with the system's shadow: Acrylic on Windows 11, the popover material (or Liquid Glass) on macOS.
/// </summary>
/// <remarks>
/// <para>
/// Both systems put a backdrop under the whole window, so the window has to be exactly the panel: no transparent margin
/// around it for a drawn shadow. While the material is on, the panel ("chrome" or "toastChrome") has the
/// <see cref="ClassName"/> class; the looks then drop that margin and shadow and make the panel see-through. If the
/// material cannot be had (Windows 10, no Acrylic, an unexpected native window), the class is removed again and the
/// look's own panel and drawn shadow are used.
/// </para>
/// <para>
/// Windows 11: the window gets a thin frame (WindowDecorations.BorderOnly, not resizable), which brings back the system
/// shadow and DWM's rounded corners (8 px, as the Windows look's panel), the Acrylic backdrop and no DWM border.
/// macOS: see <see cref="MacWindowMaterial"/>.
/// </para>
/// </remarks>
internal static class NativeMaterial
{
    /// <summary>
    /// <c>off</c> keeps the looks' own panels; <c>glass</c> uses Liquid Glass (NSGlassEffectView) on macOS 26 and later
    /// instead of the frosted popover material; <c>emulated</c> gives the settings window Avalonia's own Mica instead of
    /// the system's (see <see cref="PrepareBackdrop"/>).
    /// </summary>
    public const string Variable = "SHAPESHIFTER_NATIVE_MATERIAL";

    /// <summary>The class the panel has while it is on the system's material.</summary>
    public const string ClassName = "nativeMaterial";

    private static readonly ILogger Logger = Log.ForContext(typeof(NativeMaterial));

    /// <summary>Prepares <paramref name="window"/>, before it is first shown, to show <paramref name="panel"/> on the system's material.</summary>
    public static void Prepare(Window window, Border panel)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(panel);
        var setting = Environment.GetEnvironmentVariable(Variable);
        if (string.Equals(setting, "off", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (PlatformLook.Current == Look.Windows && OperatingSystem.IsWindows() && WindowsWindowMaterial.IsSupported)
        {
            PrepareWindows(window, panel);
        }
        else if (PlatformLook.Current == Look.MacOS && OperatingSystem.IsMacOS())
        {
            var kind = string.Equals(setting, "glass", StringComparison.OrdinalIgnoreCase)
                ? MacWindowMaterial.Kind.LiquidGlass
                : MacWindowMaterial.Kind.Vibrancy;
            PrepareMac(window, panel, kind);
        }
    }

    /// <summary>The class a window has while it is on Mica; the look then makes its background see-through.</summary>
    public const string MicaClassName = "mica";

    /// <summary>The class a window on Mica also has when the Mica is the system's own (drawn by DWM, title bar included).</summary>
    public const string SystemMicaClassName = "systemMica";

    /// <summary>
    /// Gives an ordinary window, such as the settings window, the Mica backdrop of Windows 11 as the Settings app has it;
    /// while it has it, the window has the <see cref="MicaClassName"/> class and the look makes its background see-through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// From Windows 11 22H2 (build 22621) the Mica is the system's own (DWMWA_SYSTEMBACKDROP_TYPE): DWM draws it under the
    /// native title bar and, with the frame extended over the window, under the transparent client area, so the window is
    /// one sheet of Mica with the native caption buttons, light or dark with the window's theme. Avalonia 12 has no such
    /// mode: its own Mica (used on build 22000 to 22620, or when the system's cannot be set, and with
    /// <c>SHAPESHIFTER_NATIVE_MATERIAL=emulated</c>) is drawn by Avalonia's compositor in the client area only, under an
    /// opaque title bar.
    /// </para>
    /// <para>
    /// With "Transparency effects" off the window stays opaque in the look's background, as the Settings app is then.
    /// </para>
    /// </remarks>
    public static void PrepareBackdrop(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var setting = Environment.GetEnvironmentVariable(Variable);
        if (PlatformLook.Current != Look.Windows || !OperatingSystem.IsWindows() || !WindowsWindowMaterial.IsSupported
            || string.Equals(setting, "off", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!WindowsWindowMaterial.AreTransparencyEffectsEnabled)
        {
            Logger.Information("Transparency effects are off; no Mica for {Window}", window.GetType().Name);
            return;
        }

        if (!WindowsWindowMaterial.IsSystemBackdropSupported || string.Equals(setting, "emulated", StringComparison.OrdinalIgnoreCase))
        {
            UseAvaloniaMica(window);
            return;
        }

        UseSystemMica(window);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void UseSystemMica(Window window)
    {
        // A transparent client area, so the Mica DWM draws behind it shows through. Set before the window is shown; on
        // Win32 Avalonia keeps a hinted level it already has, so the fallback below can hint again safely.
        window.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        window.Opened += (_, _) =>
        {
            var applied = false;
            try
            {
                applied = window.ActualTransparencyLevel == WindowTransparencyLevel.Transparent
                    && window.TryGetPlatformHandle() is { } handle
                    && WindowsWindowMaterial.TryApplyMainWindowMica(handle.Handle);
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                Logger.Debug(exception, "Could not set the system backdrop");
            }

            if (applied)
            {
                window.Classes.Set(SystemMicaClassName, true);
                window.Classes.Set(MicaClassName, true);
                return;
            }

            Logger.Information("No system Mica for {Window} ({Level}); using Avalonia's", window.GetType().Name, window.ActualTransparencyLevel);
            window.TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None];
            window.Classes.Set(MicaClassName, window.ActualTransparencyLevel == WindowTransparencyLevel.Mica);
        };
    }

    private static void UseAvaloniaMica(Window window)
    {
        // Where Mica is not granted the level falls back to None or Transparent: the window keeps the look's opaque
        // background (and TransparencyBackgroundFallback) without the class.
        window.TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.None];
        window.Opened += (_, _) => window.Classes.Set(MicaClassName, window.ActualTransparencyLevel == WindowTransparencyLevel.Mica);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void PrepareWindows(Window window, Border panel)
    {
        if (!WindowsWindowMaterial.AreTransparencyEffectsEnabled)
        {
            // Avalonia would still report Acrylic, but Windows draws no blur: keep the look's own near-opaque panel.
            Logger.Information("Transparency effects are off; no Acrylic for {Window}", window.GetType().Name);
            return;
        }

        window.WindowDecorations = WindowDecorations.BorderOnly;
        window.CanResize = false;
        Win32Properties.SetWindowCornerPreference(window, Win32Properties.WindowCornerPreference.Round);
        window.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent];
        panel.Classes.Add(ClassName);
        window.Opened += (_, _) =>
        {
            if (window.ActualTransparencyLevel != WindowTransparencyLevel.AcrylicBlur)
            {
                Logger.Information("No Acrylic for {Window} ({Level}); using the look's own panel", window.GetType().Name, window.ActualTransparencyLevel);
                panel.Classes.Remove(ClassName);
                window.WindowDecorations = WindowDecorations.None;
                return;
            }

            try
            {
                if (window.TryGetPlatformHandle() is { } handle)
                {
                    WindowsWindowMaterial.HideBorder(handle.Handle);
                }
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                Logger.Debug(exception, "Could not hide the window border");
            }
        };
    }

    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static void PrepareMac(Window window, Border panel, MacWindowMaterial.Kind kind)
    {
        // The window keeps the Transparent level its XAML asks for. Do not set the hint again: Avalonia 12.1 (Avalonia.Native
        // SetTransparencyLevelHint) skips a level equal to the current one and falls through to an opaque window.
        panel.Classes.Add(ClassName);
        window.Opened += (_, _) =>
        {
            // The panel has dropped its margin by now, so after layout the window is the panel.
            window.UpdateLayout();
            var attached = false;
            try
            {
                if (window.TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle handle)
                {
                    var size = window.ClientSize;
                    attached = MacWindowMaterial.Attach(
                        handle.NSWindow, handle.NSView, panel.CornerRadius.TopLeft, size.Width, size.Height, kind, out var applied);
                    if (attached && applied != kind)
                    {
                        Logger.Information("{Wanted} is not available; using {Applied}", kind, applied);
                    }
                }
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                Logger.Warning(exception, "Could not put the system material behind {Window}", window.GetType().Name);
            }

            if (!attached)
            {
                Logger.Information("Could not put the system material behind {Window}; using the look's own panel", window.GetType().Name);
                panel.Classes.Remove(ClassName);
                return;
            }

            RefreshShadowAfterRender(window);
        };

        // macOS derives the shadow from the drawn window, so recompute it whenever the window's size changes.
        window.SizeChanged += (_, _) => RefreshShadowAfterRender(window);
        window.Closed += (_, _) =>
        {
            if (window.TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle handle)
            {
                MacWindowMaterial.Forget(handle.NSWindow);
            }
        };
    }

    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static void RefreshShadowAfterRender(Window window)
    {
        // After the next frame has been drawn: Avalonia's own refreshes run before it.
        Dispatcher.UIThread.Post(
            () =>
            {
                if (window.IsVisible && window.TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle handle && MacWindowMaterial.IsAttached(handle.NSWindow))
                {
                    MacWindowMaterial.RefreshShadow(handle.NSWindow);
                }
            },
            DispatcherPriority.Background);
    }
}
