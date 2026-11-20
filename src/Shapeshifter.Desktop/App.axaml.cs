using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Coaching;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Persistence;
using Shapeshifter.Core.Previews;
using Shapeshifter.Core.Settings;
using Shapeshifter.Core.Support;
using Shapeshifter.Core.Updates;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Desktop.ViewModels;
using Shapeshifter.Desktop.Views;
using Shapeshifter.Platform;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Desktop;

#pragma warning disable CA1001 // Disposed in Shutdown(), driven by the application lifetime.
public partial class App : Application
{
    /// <summary>The first update check waits this long, so it does not compete with starting up.</summary>
    private static readonly TimeSpan FirstUpdateCheck = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

    private readonly ILogger logger = Log.ForContext<App>();
    private IClassicDesktopStyleApplicationLifetime? desktop;
    private PlatformServices? platform;
    private ShapeshifterEngine? engine;
    private ClipboardMonitor? monitor;
    private SettingsStore? settings;
    private ClipboardHistory? history;
    private HistoryPersistence? savedHistory;
    private SettingsWindow? settingsWindow;
    private HttpClient? http;
    private TrayIcon? trayIcon;
    private PickerPresenter? picker;
    private HttpClient? updateHttp;
    private Updater? updater;
    private DispatcherTimer? restartTimer;

    /// <summary>Set by <see cref="Program"/> before Avalonia starts.</summary>
    internal static AppPaths Paths { get; set; } = AppPaths.CreateDefault();

    /// <summary>What the installer found at startup (set by <see cref="Program"/>); null when it did not run.</summary>
    internal static StartupResult? Installed { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            desktop = lifetime;
            PlatformLook.Apply(this);
            AccentPalette.Attach(this);
            // ShutdownRequested is only raised for platform-initiated shutdowns (logout, Cmd+Q), not for
            // lifetime.Shutdown() from the tray's Quit item; Exit is raised for every shutdown.
            lifetime.Exit += (_, _) => Shutdown();
            Dispatcher.UIThread.Post(async () => await StartAsync().ConfigureAwait(true));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartAsync()
    {
        try
        {
            CleanTemporaryFiles();
            settings = new SettingsStore(Paths.SettingsFile, logger);
            history = new ClipboardHistory(settings.Current.MaxHistoryItems);

            // A never-shown window gives access to the clipboard and the storage provider.
            var clipboardHost = new Window { Width = 1, Height = 1, ShowInTaskbar = false, WindowDecorations = WindowDecorations.None };
            var backend = new AvaloniaClipboardBackend(clipboardHost, logger);

            platform = await PlatformServices.CreateAsync(logger).ConfigureAwait(true);
            foreach (var warning in platform.Warnings)
            {
                logger.Warning("{Warning}", warning);
            }

            var changes = platform.CreateClipboardChangeSource(backend, logger);
            monitor = new ClipboardMonitor(backend, changes, history, logger, platform.SourceLocator);
            var pins = new PinnedItemStore(Paths.PinnedDirectory, logger);
            foreach (var pinned in await pins.LoadAllAsync().ConfigureAwait(true))
            {
                history.AddPinned(pinned);
            }

            // The unpinned history, when "Remember history after restart" is on; before the first capture, so the
            // current clipboard lands on top of it.
            savedHistory = new HistoryPersistence(history, Paths.HistoryDirectory, () => settings.Current.RememberHistory, logger);
            await savedHistory.LoadAsync().ConfigureAwait(true);
            savedHistory.Start();

            http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Shapeshifter/" + typeof(App).Assembly.GetName().Version?.ToString(3));
            var keyboard = platform.KeyboardHook;
            keyboard.PasteShortcut = settings.Current.PasteShortcut;
            var context = new ActionContext
            {
                Clipboard = monitor,
                Paste = platform.PasteSimulator,
                History = history,
                PinnedItems = pins,
                Launcher = platform.Launcher,
                Paths = Paths,
                Http = http,
                Logger = logger,
            };

            var windowFocus = platform.WindowFocus is { } focus ? new UiThreadWindowFocus(focus) : null;
            picker = new PickerPresenter(
                platform.CursorLocator,
                new PickerVisuals(platform.FileIcons, logger),
                keyboard.ModifierName,
                logger,
                windowFocus,
                new LinkedImageLoader(http, logger));
            var coach = new CopyCoach(platform.SourceLocator, () => keyboard.ModifierName, () => settings.Current.CoachingTips);
            engine = new ShapeshifterEngine(
                history,
                monitor,
                platform.KeyboardHook,
                ActionCatalog.CreateDefault(),
                context,
                picker,
                new CopyNotifier(settings),
                logger,
                coach: coach,
                focusTracker: windowFocus)
            {
                PasteDelay = TimeSpan.FromMilliseconds(settings.Current.PasteDelayMilliseconds),
            };

            settings.Changed += (_, current) =>
            {
                engine.PasteDelay = TimeSpan.FromMilliseconds(current.PasteDelayMilliseconds);
                if (keyboard.PasteShortcut != current.PasteShortcut)
                {
                    keyboard.PasteShortcut = current.PasteShortcut;
                    picker.SetModifierName(keyboard.ModifierName);
                }

                history.MaxUnpinnedItems = current.MaxHistoryItems;
                savedHistory.RequestSync();
            };

            DiagnosticsStateWriter.TryCreate(history, picker, engine);
            await engine.StartAsync().ConfigureAwait(true);
            CreateTrayIcon();
            ConfigureInstalledCopy();
            StartUpdater();

            logger.Information("Shapeshifter is ready");
            Console.WriteLine("SHAPESHIFTER_READY");

            if (platform.Warnings.Count > 0 && Environment.GetEnvironmentVariable(DiagnosticsStateWriter.Variable) is null)
            {
                ShowSettings();
            }
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Shapeshifter could not start");
            await ShowStartupErrorAsync(exception).ConfigureAwait(true);
            desktop?.Shutdown(3);
        }
    }

    private void CreateTrayIcon()
    {
        try
        {
            using var iconStream = AssetLoader.Open(PlatformLook.Asset(TrayIconName()));
            var menu = new NativeMenu();
            menu.Items.Add(MenuItem(Strings.TraySettings, ShowSettings));
            menu.Items.Add(MenuItem(Strings.SettingsOpenLogFolder, () => platform?.Launcher.OpenFolder(Paths.LogDirectory)));
            menu.Items.Add(MenuItem(Strings.SettingsClearHistory, () => history?.Clear()));
            menu.Items.Add(MenuItem(Strings.TrayCheckForUpdates, CheckForUpdates));
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(MenuItem(Strings.TrayReportProblem, ReportProblem));
            menu.Items.Add(MenuItem(Localizer.Format(Strings.TrayAbout, ReleaseVersion.Current()), ShowSettings));
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(MenuItem(Strings.TrayQuit, () => desktop?.Shutdown()));

            trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new Bitmap(iconStream)),
                ToolTipText = "Shapeshifter " + ReleaseVersion.Current(),
                Menu = menu,
                IsVisible = true,
            };

            // The menu bar draws a template icon in its own colour, light or dark, as it does for system icons.
            MacOSProperties.SetIsTemplateIcon(trayIcon, PlatformLook.Current == Look.MacOS);
            trayIcon.Clicked += (_, _) => ShowSettings();

            // Follow the taskbar or panel when the user switches between light and dark.
            if (PlatformSettings is { } platformSettings)
            {
                platformSettings.ColorValuesChanged += (_, _) => Dispatcher.UIThread.Post(UpdateTrayIcon);
            }

            TrayIcon.SetIcons(this, [trayIcon]);
        }
        catch (Exception exception)
        {
            // Some Linux desktops have no system tray; Shapeshifter keeps working without it.
            logger.Warning(exception, "Could not create the tray icon");
        }
    }

    /// <summary>
    /// The tray icon of the look, in the tone that reads on the panel: a dark glyph on a light taskbar or panel and a
    /// light glyph on a dark one (<c>TrayLight.png</c> and <c>TrayDark.png</c>, named after the panel).
    /// </summary>
    private string TrayIconName()
    {
        if (PlatformLook.Current == Look.Windows && OperatingSystem.IsWindows())
        {
            // The taskbar follows "Windows mode", which apps cannot see through the theme variant.
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("SystemUsesLightTheme") is int and not 0 ? "TrayLight.png" : "TrayDark.png";
            }
            catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return "Tray.png";
            }
        }

        if (PlatformLook.Current == Look.Linux)
        {
            var variant = PlatformSettings?.GetColorValues().ThemeVariant ?? PlatformThemeVariant.Light;
            return LinuxPanelIsDark(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"), variant) ? "TrayDark.png" : "TrayLight.png";
        }

        return "Tray.png";
    }

    /// <summary>
    /// Guesses the tone of the Linux panel that shows the tray icon. GNOME Shell (also Ubuntu's) and Pantheon keep
    /// their top bar dark in light mode too; other desktops (KDE Plasma, XFCE, Cinnamon, MATE, LXQt) theme their panel
    /// with the light or dark colour scheme, which the desktop portal reports as the theme variant.
    /// </summary>
    internal static bool LinuxPanelIsDark(string? currentDesktop, PlatformThemeVariant variant)
    {
        var desktops = (currentDesktop ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (desktops.Any(desktop => desktop.Equals("GNOME", StringComparison.OrdinalIgnoreCase) || desktop.Equals("Pantheon", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return variant == PlatformThemeVariant.Dark;
    }

    private void UpdateTrayIcon()
    {
        if (trayIcon is null)
        {
            return;
        }

        try
        {
            using var stream = AssetLoader.Open(PlatformLook.Asset(TrayIconName()));
            trayIcon.Icon = new WindowIcon(new Bitmap(stream));
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not update the tray icon");
        }
    }

    private static NativeMenuItem MenuItem(string header, Action action)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => action();
        return item;
    }

    /// <summary>Opens a new GitHub issue with the version and operating system filled in.</summary>
    private void ReportProblem() => platform?.Launcher.OpenUrl(
        ProblemReport.CreateIssueUrl(Installation.App, ProblemReport.DescribeCurrentEnvironment(Installation.App.Name)));

    private void ShowSettings()
    {
        if (settings is null || history is null || platform is null)
        {
            return;
        }

        if (settingsWindow is not null)
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow
        {
            Icon = new WindowIcon(AssetLoader.Open(PlatformLook.Asset("Application.png"))),
            DataContext = new SettingsViewModel(
                settings, platform.Autostart, history, platform.Launcher, Paths, () => platform.KeyboardHook.ModifierName, platform.Warnings, updater,
                platform.KeyboardHook.ShortcutModifierNames),
        };
        settingsWindow.Closed += (_, _) =>
        {
            (settingsWindow?.DataContext as SettingsViewModel)?.Detach();
            settingsWindow = null;
        };
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    /// <summary>
    /// The installed copy starts at login: switched on when Shapeshifter was just installed, and pointed at the
    /// installed executable when it was switched on for a copy elsewhere.
    /// </summary>
    private void ConfigureInstalledCopy()
    {
        if (Installed is not { IsInstalledCopy: true } installed || platform is not { Autostart.IsSupported: true })
        {
            return;
        }

        try
        {
            if (installed.FirstInstall || platform.Autostart.IsEnabled)
            {
                platform.Autostart.SetEnabled(true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            logger.Warning(exception, "Could not set up start at login");
        }
    }

    private void StartUpdater()
    {
        if (StoreInstall.IsActive)
        {
            // The Microsoft Store or the Mac App Store updates this copy; store policies forbid updating it from GitHub.
            return;
        }

        updateHttp = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        updateHttp.DefaultRequestHeaders.UserAgent.ParseAdd("Shapeshifter/" + ReleaseVersion.Current());
        updater = new Updater(
            Installation.App,
            RunningApp.Current(),
            Installation.Layout(Paths),
            updateHttp,
            new Installation.UpdateLog(logger),
            () => settings?.Current.AutomaticUpdates == true);
        updater.UpdateReady += (_, _) => Dispatcher.UIThread.Post(RestartWhenIdle);
        updater.Start(FirstUpdateCheck, UpdateCheckInterval);
    }

    private void CheckForUpdates()
    {
        if (updater is not { IsSupported: true })
        {
            platform?.Launcher.OpenUrl(Installation.App.ReleasesPage);
            return;
        }

        ShowSettings();
        _ = updater.CheckNowAsync();
    }

    /// <summary>Restarts into the new version once the picker and the settings window are closed.</summary>
    private void RestartWhenIdle()
    {
        if (updater is not { State: UpdateState.ReadyToRestart } || desktop is null)
        {
            return;
        }

        if (picker?.IsVisible == true || settingsWindow is not null)
        {
            restartTimer ??= new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (_, _) => RestartWhenIdle());
            restartTimer.Start();
            return;
        }

        restartTimer?.Stop();
        logger.Information("Restarting into Shapeshifter {Version}", updater.PendingVersion);
        try
        {
            updater.Restart([]);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.Warning(exception, "Could not start the new version; it starts the next time Shapeshifter does");
            return;
        }

        desktop.Shutdown();
    }

    private static async Task ShowStartupErrorAsync(Exception exception)
    {
        var tcs = new TaskCompletionSource();
        var close = new Button { Content = Strings.StartupErrorQuit, HorizontalAlignment = HorizontalAlignment.Right };
        var window = new Window
        {
            Title = Strings.StartupErrorTitle,
            FlowDirection = Localizer.IsRightToLeft ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = exception.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new TextBlock { Text = Localizer.Format(Strings.StartupErrorDetails, Paths.LogDirectory), TextWrapping = Avalonia.Media.TextWrapping.Wrap, Opacity = 0.7 },
                    close,
                },
            },
        };
        NativeMaterial.PrepareBackdrop(window);
        close.Click += (_, _) => window.Close();
        window.Closed += (_, _) => tcs.TrySetResult();
        if (Environment.GetEnvironmentVariable(DiagnosticsStateWriter.Variable) is not null)
        {
            return;
        }

        window.Show();
        await tcs.Task.ConfigureAwait(true);
    }

    private void Shutdown()
    {
        logger.Information("Shutting down");
        try
        {
            engine?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
            savedHistory?.Stop();
            savedHistory?.FlushAsync().Wait(TimeSpan.FromSeconds(3));
            monitor?.Dispose();
            platform?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
            trayIcon?.Dispose();
            http?.Dispose();
            updater?.Dispose();
            updateHttp?.Dispose();
        }
        catch (Exception exception)
        {
            logger.Warning(exception, "Error during shutdown");
        }
    }

    private void CleanTemporaryFiles()
    {
        try
        {
            if (Directory.Exists(Paths.TempDirectory))
            {
                Directory.Delete(Paths.TempDirectory, recursive: true);
            }

            Directory.CreateDirectory(Paths.TempDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.Warning(exception, "Could not clean {Folder}", Paths.TempDirectory);
        }
    }
}
