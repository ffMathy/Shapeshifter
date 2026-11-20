using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;
using Shapeshifter.Platform.Input;
using Shapeshifter.Platform.Linux;
using Shapeshifter.Platform.MacOS;
using Shapeshifter.Platform.Windows;
using SharpHook.Data;
using SharpHook.Providers;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Platform;

/// <summary>All operating system services Shapeshifter needs, for the current platform.</summary>
public sealed class PlatformServices : IAsyncDisposable
{
    private readonly X11Integration? x11;
    private readonly SharpHookKeyboardService keyboard;
    private readonly List<IDisposable> owned = [];

    private PlatformServices(ILogger logger, X11Integration? x11, IReadOnlyList<string> warnings)
    {
        this.x11 = x11;
        Warnings = warnings;
        keyboard = new SharpHookKeyboardService(logger, KeyMap.ForCurrentPlatform(), x11);
        Launcher = new Launcher(logger);
        Autostart = CreateAutostart();
        CursorLocator = CreateCursorLocator(x11);
        SourceLocator = CreateSourceLocator(logger);
        FileIcons = CreateFileIconProvider(logger);
        WindowFocus = CreateWindowFocus(logger, x11);
    }

    public IKeyboardHook KeyboardHook => keyboard;

    public IPasteSimulator PasteSimulator => keyboard;

    public ILauncher Launcher { get; }

    public IAutostartService Autostart { get; }

    public ICursorLocator CursorLocator { get; }

    /// <summary>Which application a copy came from, or <see langword="null"/> where that cannot be known.</summary>
    public IClipboardSourceLocator? SourceLocator { get; }

    /// <summary>The operating system's icons for files and folders, or <see langword="null"/> where unavailable.</summary>
    public IFileIconProvider? FileIcons { get; }

    /// <summary>Moves the keyboard focus between the picker and the window the user works in, where possible.</summary>
    public IWindowFocus? WindowFocus { get; }

    /// <summary>Limitations of the current session that the user should know about.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public static string PlatformName =>
        OperatingSystem.IsWindows() ? "Windows" :
        OperatingSystem.IsMacOS() ? "macOS" :
        OperatingSystem.IsLinux() ? (IsWaylandSession ? "Linux (Wayland)" : "Linux (X11)") :
        Environment.OSVersion.Platform.ToString();

    private static bool IsWaylandSession =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
        || string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates and initializes the services. On Linux this must run after the UI toolkit connected to X11,
    /// so that the X error handler is chained correctly.
    /// </summary>
    public static async Task<PlatformServices> CreateAsync(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var warnings = new List<string>();
        X11Integration? x11 = null;

        if (OperatingSystem.IsLinux())
        {
            if (!X11Integration.IsAvailable)
            {
                throw new PlatformNotSupportedException(Strings.PlatformNeedsX11);
            }

            // Use the XRecord/XTest backend even on Wayland sessions: it works through XWayland without root access.
            var mode = UioHookProvider.Instance.SetLinuxMode(LinuxMode.XRecord);
            if (mode != UioHookResult.Success)
            {
                logger.Warning("Could not select the XRecord input backend: {Result}", mode);
            }

            if (IsWaylandSession)
            {
                warnings.Add(Strings.PlatformWayland);
            }

            x11 = new X11Integration(logger);
            await x11.StartAsync().ConfigureAwait(false);
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (!UioHookProvider.Instance.IsAxApiEnabled(false))
            {
                warnings.Add(Strings.PlatformAccessibility);
            }
        }
        else if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(Localizer.Format(Strings.PlatformNotSupported, Environment.OSVersion));
        }

        var services = new PlatformServices(logger, x11, warnings);
        if (x11 is not null)
        {
            services.owned.Add(x11);
        }

        return services;
    }

    /// <summary>
    /// Returns the best available clipboard change notification for this platform. Falls back to
    /// polling <paramref name="backend"/> when there is no native mechanism.
    /// </summary>
    public IClipboardChangeSource CreateClipboardChangeSource(IClipboardBackend backend, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(backend);
        IClipboardChangeSource source;
#pragma warning disable CA1416 // The lambdas below are only created inside the matching OS checks.
        if (OperatingSystem.IsWindows())
        {
            source = new PollingClipboardChangeSource(
                _ => Task.FromResult(WindowsNative.GetClipboardSequenceNumber().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                TimeSpan.FromMilliseconds(150),
                logger);
        }
        else if (OperatingSystem.IsMacOS())
        {
            source = new PollingClipboardChangeSource(
                _ => Task.FromResult(MacNative.GetPasteboardChangeCount().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                TimeSpan.FromMilliseconds(200),
                logger);
        }
#pragma warning restore CA1416
        else if (x11 is { SupportsClipboardNotifications: true })
        {
            return x11;
        }
        else
        {
            source = new PollingClipboardChangeSource(backend.GetChangeTokenAsync, TimeSpan.FromMilliseconds(500), logger);
        }

        owned.Add(source);
        return source;
    }

    public ValueTask DisposeAsync()
    {
        keyboard.Dispose();
        foreach (var disposable in Enumerable.Reverse(owned))
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>The start-at-login service of this platform; usable without the rest of the platform services.</summary>
    public static IAutostartService CreateAutostart()
    {
        if (Core.Updates.StoreInstall.IsActive)
        {
            // An MSIX package's registry writes and a sandboxed app's ~/Library/LaunchAgents stay private to the app, so
            // neither Windows nor launchd would see the entry. The Store package starts at login through its manifest's
            // startup task instead, which the user switches in Settings > Apps > Startup.
            return new UnsupportedAutostartService();
        }

        if (OperatingSystem.IsWindows())
        {
            return new WindowsAutostartService();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new LaunchAgentAutostartService();
        }

        return OperatingSystem.IsLinux() ? new XdgAutostartService() : new UnsupportedAutostartService();
    }

    private static ICursorLocator CreateCursorLocator(X11Integration? x11)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsCursorLocator();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacCursorLocator();
        }

        return x11 is not null ? x11 : new NoCursorLocator();
    }

    private IClipboardSourceLocator? CreateSourceLocator(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsClipboardSourceLocator(logger);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacClipboardSourceLocator(logger);
        }

        if (x11 is null)
        {
            return null;
        }

        DesktopEntryIndex.Warm();
        return new X11ClipboardSourceLocator(logger);
    }

    private static IWindowFocus? CreateWindowFocus(ILogger logger, X11Integration? x11)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsWindowFocus(logger);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacWindowFocus();
        }

        return x11 is null ? null : new X11WindowFocus(x11);
    }

    private static IFileIconProvider? CreateFileIconProvider(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsFileIconProvider(logger);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacFileIconProvider(logger);
        }

        return OperatingSystem.IsLinux() ? new FreedesktopFileIconProvider(logger) : null;
    }

    private sealed class NoCursorLocator : ICursorLocator
    {
        public (int X, int Y)? GetCursorPosition() => null;
    }
}
