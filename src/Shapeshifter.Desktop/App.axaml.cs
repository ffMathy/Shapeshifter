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

            var picker = new PickerPresenter(platform.CursorLocator, new PickerVisuals(platform.FileIcons, logger), logger);
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
            using var iconStream = AssetLoader.Open(new Uri("avares://Shapeshifter/Assets/Tray.png"));
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
            trayIcon.Clicked += (_, _) => ShowSettings();
            TrayIcon.SetIcons(this, [trayIcon]);
        }
        catch (Exception exception)
        {
            // Some Linux desktops have no system tray; Shapeshifter keeps working without it.
            logger.Warning(exception, "Could not create the tray icon");
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
