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
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Persistence;
using Shapeshifter.Core.Settings;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Desktop.ViewModels;
using Shapeshifter.Desktop.Views;
using Shapeshifter.Platform;

namespace Shapeshifter.Desktop;

#pragma warning disable CA1001 // Disposed in Shutdown(), driven by the application lifetime.
public partial class App : Application
{
    private static readonly Uri ReleasesPage = new("https://github.com/ffMathy/Shapeshifter/releases");

    private readonly ILogger logger = Log.ForContext<App>();
    private IClassicDesktopStyleApplicationLifetime? desktop;
    private PlatformServices? platform;
    private ShapeshifterEngine? engine;
    private ClipboardMonitor? monitor;
    private SettingsStore? settings;
    private ClipboardHistory? history;
    private SettingsWindow? settingsWindow;
    private HttpClient? http;
    private TrayIcon? trayIcon;

    /// <summary>Set by <see cref="Program"/> before Avalonia starts.</summary>
    internal static AppPaths Paths { get; set; } = AppPaths.CreateDefault();

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

            http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Shapeshifter/" + typeof(App).Assembly.GetName().Version?.ToString(3));
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

            var picker = new PickerPresenter(platform.CursorLocator, new PickerVisuals(platform.FileIcons, logger), platform.KeyboardHook.ModifierName, logger);
            engine = new ShapeshifterEngine(
                history, monitor, platform.KeyboardHook, ActionCatalog.CreateDefault(), context, picker, new CopyNotifier(settings), logger)
            {
                PasteDelay = TimeSpan.FromMilliseconds(settings.Current.PasteDelayMilliseconds),
            };

            settings.Changed += (_, current) =>
            {
                engine.PasteDelay = TimeSpan.FromMilliseconds(current.PasteDelayMilliseconds);
                history.MaxUnpinnedItems = current.MaxHistoryItems;
            };

            DiagnosticsStateWriter.TryCreate(history, picker, engine);
            await engine.StartAsync().ConfigureAwait(true);
            CreateTrayIcon();

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
            menu.Items.Add(MenuItem("Settings…", ShowSettings));
            menu.Items.Add(MenuItem("Open log folder", () => platform?.Launcher.OpenFolder(Paths.LogDirectory)));
            menu.Items.Add(MenuItem("Clear history", () => history?.Clear()));
            menu.Items.Add(MenuItem("Check for updates…", () => platform?.Launcher.OpenUrl(ReleasesPage)));
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(MenuItem("Quit Shapeshifter", () => desktop?.Shutdown()));

            trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new Bitmap(iconStream)),
                ToolTipText = "Shapeshifter",
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
                settings, platform.Autostart, history, platform.Launcher, Paths, platform.KeyboardHook.ModifierName, platform.Warnings),
        };
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    private static async Task ShowStartupErrorAsync(Exception exception)
    {
        var tcs = new TaskCompletionSource();
        var close = new Button { Content = "Quit", HorizontalAlignment = HorizontalAlignment.Right };
        var window = new Window
        {
            Title = "Shapeshifter could not start",
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
                    new TextBlock { Text = $"Details were written to {Paths.LogDirectory}.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, Opacity = 0.7 },
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
            monitor?.Dispose();
            platform?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
            trayIcon?.Dispose();
            http?.Dispose();
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
