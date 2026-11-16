using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Desktop.ViewModels;
using Shapeshifter.Desktop.Views;
using Path = System.IO.Path;

namespace Shapeshifter.ScreenshotGenerator;

/// <summary>
/// Renders the real picker window, with sample history items, over a desktop background and saves the result as
/// the screenshots in <c>assets/screenshots</c>. Run it after changing the picker so the screenshots stay current:
/// <c>scripts/generate-screenshots.sh</c>.
/// </summary>
/// <remarks>
/// Rendering is headless (Avalonia.Headless with Skia) and uses the bundled Inter font, so the output is the same on
/// every machine and in CI. All sample artwork, including application and file icons, is drawn here.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// Screenshot name, look, accent colour (null: the look's own) and display scaling. The coloured ones are used by
    /// the website (shapeshifter.app) and taken at 125 %, as the original screenshots were; the PAD one is smaller.
    /// </summary>
    private static readonly (string Name, Look Look, string? Accent, double Scale, int Width, int Height)[] Screenshots =
    [
        ("windows", Look.Windows, null, 1.25, 1400, 820),
        ("macos", Look.MacOS, null, 1.25, 1400, 820),
        ("linux", Look.Linux, null, 1.25, 1400, 820),
        ("blue", Look.Windows, "#0063B1", 1.25, 1400, 820),
        ("green", Look.Windows, "#107C10", 1.25, 1400, 820),
        ("purple", Look.Windows, "#881798", 1.25, 1400, 820),
        ("red", Look.Windows, "#D41124", 1.25, 1400, 820),
        ("pad", Look.Windows, null, 0.9, 1000, 600),
    ];

    /// <summary>
    /// <c>[output-folder] [--surfaces]</c>. With <c>--surfaces</c>, the settings window and the copy notification of
    /// each look are rendered as well (<c>settings-&lt;look&gt;.PNG</c>, <c>toast-&lt;look&gt;.PNG</c>), for reviewing a look.
    /// </summary>
    public static int Main(string[] args)
    {
        var surfaces = args.Contains("--surfaces");
        var folder = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        var output = Path.GetFullPath(folder ?? Path.Combine("assets", "screenshots"));
        Directory.CreateDirectory(output);

        AppBuilder.Configure<Shapeshifter.Desktop.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

        foreach (var (name, look, accent, scale, width, height) in Screenshots)
        {
            var path = Path.Combine(output, name + ".PNG");
            UseLook(look, accent);
            RenderPicker(look, scale, width, height, path);
            Console.WriteLine($"Wrote {path}");
        }

        if (surfaces)
        {
            foreach (var look in Enum.GetValues<Look>())
            {
                UseLook(look, null);
                var settings = Path.Combine(output, $"settings-{look.ToString().ToLowerInvariant()}.PNG");
                RenderWindow(CreateSettingsWindow(), look, settings);
                Console.WriteLine($"Wrote {settings}");
                var toast = Path.Combine(output, $"toast-{look.ToString().ToLowerInvariant()}.PNG");
                var toastWindow = new ToastWindow();
                toastWindow.SetContent(5, "Shapeshifter is a freeware clipboard manager, originally developed by defunct Flamefusion company");
                RenderWindow(toastWindow, look, toast);
                Console.WriteLine($"Wrote {toast}");
            }
        }

        return 0;
    }

    private static void UseLook(Look look, string? accent)
    {
        var application = Application.Current!;
        PlatformLook.Apply(application, look);
        var color = accent ?? PlatformLook.Resource(application, "LookDefaultAccent", "#0067C0");
        AccentPalette.Apply(application, Color.Parse(color));
    }

    private static void RenderPicker(Look look, double scale, int width, int height, string path)
    {
        var viewModel = new PickerViewModel(look == Look.MacOS ? "Cmd" : "Ctrl") { IsOpaque = false, MaxListHeight = 10_000 };
        var visuals = new PickerVisuals(new SampleFileIcons(), Log.Logger);
        var photo = SampleArt.Banner(900, 600);
        using var thumbnail = ToBitmap(photo);
        foreach (var item in SampleItems(look, photo))
        {
            viewModel.Items.Add(new PickerItemViewModel(item, item.Kind == ClipboardContentKind.Image ? thumbnail : null, visuals));
        }

        viewModel.Actions.Add(new PickerActionViewModel(new PickerActionView("Paste", "Pastes clipboard contents as-is."), isDefault: true));
        viewModel.Actions.Add(new PickerActionViewModel(new PickerActionView("Copy as file", "Copy this text to the clipboard as a file."), isDefault: false));
        viewModel.Actions.Add(new PickerActionViewModel(new PickerActionView("Paste as plain text", "Pastes clipboard contents as plain text with no formatting."), isDefault: false));
        viewModel.Actions.Add(new PickerActionViewModel(new PickerActionView(
            "Pin to clipboard",
            "Pins this item so that it always stays in the top, and gets saved to your harddrive so that it is there the next time you open Shapeshifter."), isDefault: false));
        viewModel.SelectedIndex = 0;
        viewModel.SelectedActionIndex = 0;
        viewModel.UpdateActionsHeader();

        // The picker's own window cannot be captured over a desktop, so its content is moved into a window that
        // also shows a desktop background behind it.
        var picker = new PickerWindow { DataContext = viewModel };
        var content = (Control)picker.Content!;
        picker.Content = null;
        content.DataContext = viewModel;

        using var background = ToBitmap(SampleArt.Desktop(look, width, height));
        var host = new Window
        {
            Width = width,
            Height = height,
            WindowDecorations = WindowDecorations.None,
            FontFamily = picker.FontFamily,
            Content = new Panel
            {
                Children =
                {
                    new Image { Source = background, Stretch = Stretch.Fill },
                    new LayoutTransformControl
                    {
                        LayoutTransform = new ScaleTransform(scale, scale),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, height * 0.08, 0, 0),
                        Child = content,
                    },
                },
            },
        };

        Capture(host, path);
        picker.Close();
    }

    /// <summary>Renders a window on its own, at 125 %, over a plain backdrop of the look.</summary>
    private static void RenderWindow(Window window, Look look, string path)
    {
        var content = (Control)window.Content!;
        window.Content = null;
        content.DataContext = window.DataContext;
        if (!double.IsNaN(window.Width))
            content.Width = window.Width - (content.Margin.Left + content.Margin.Right);
        var host = new Window
        {
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowDecorations = WindowDecorations.None,
            FontFamily = window.FontFamily,
            Background = new SolidColorBrush(Color.Parse(look switch { Look.MacOS => "#DCDDE6", Look.Linux => "#DEDDDA", _ => "#D8E2F0" })),
            Content = new LayoutTransformControl
            {
                LayoutTransform = new ScaleTransform(1.25, 1.25),
                Margin = new Thickness(40),
                Child = new Border
                {
                    Background = window.Background,
                    Child = content,
                },
            },
        };

        Capture(host, path);
        window.Close();
    }

    private static void Capture(Window host, string path)
    {
        // Greyscale antialiasing: subpixel (LCD) antialiasing would tint thin text, and depends on the screen.
        TextOptions.SetTextRenderingMode(host, TextRenderingMode.Antialias);
        RenderOptions.SetBitmapInterpolationMode(host, BitmapInterpolationMode.HighQuality);
        host.Show();
        for (var i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        using var frame = host.CaptureRenderedFrame() ?? throw new InvalidOperationException("Nothing was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
        host.Close();
    }

    private static SettingsWindow CreateSettingsWindow()
    {
        var root = Path.Combine(Path.GetTempPath(), "shapeshifter-screenshots");
        var paths = AppPaths.ForRoot(root);
        var store = new SettingsStore(Path.Combine(root, "settings.json"));
        return new SettingsWindow
        {
            DataContext = new SettingsViewModel(
                store, new SampleAutostart(), new ClipboardHistory(25), new SampleLauncher(), paths,
                PlatformLook.Current == Look.MacOS ? "Cmd" : "Ctrl", []),
        };
    }

    /// <summary>The items shown in the screenshots, copied from applications typical of the platform.</summary>
    private static IEnumerable<ClipboardItem> SampleItems(Look look, byte[] photo)
    {
        const string article = "Shapeshifter is a freeware clipboard manager, originally developed by defunct Flamefusion company " +
            "and later released as open source.";
        var browser = SampleArt.BrowserIcon();
        var editor = SampleArt.EditorIcon();
        var desktop = look switch
        {
            Look.MacOS => "/Users/shapeshifter/Desktop/",
            Look.Linux => "/home/shapeshifter/Desktop/",
            _ => "C:/Users/Shapeshifter/Desktop/",
        };
        var (browserName, page) = look switch
        {
            Look.MacOS => ("Safari", "Shapeshifter (software) – Wikipedia"),
            Look.Linux => ("Firefox", "Shapeshifter (software) – Wikipedia — Mozilla Firefox"),
            _ => ("Google Chrome", "Shapeshifter (software) - Wikipedia – Google Chrome"),
        };
        var newFile = look switch { Look.MacOS => "Untitled.txt", Look.Linux => "notes.txt", _ => "New Text Document.txt" };

        yield return new ClipboardItem(new ClipboardSnapshot { Text = article }, source: new ClipboardSource(browserName, page, browser));
        yield return new ClipboardItem(new ClipboardSnapshot { Image = photo }, source: new ClipboardSource(browserName, page, browser));
        yield return new ClipboardItem(new ClipboardSnapshot
        {
            Files = [desktop + "Projects", desktop + "Notes.txt", desktop + "Holiday.mp4", desktop + "Sunset.png"],
        });
        yield return new ClipboardItem(new ClipboardSnapshot { Files = [desktop + newFile] });
        yield return new ClipboardItem(
            new ClipboardSnapshot { Text = "dotnet run --project src/Shapeshifter.Desktop" },
            source: new ClipboardSource("Terminal", "Terminal — shapeshifter", editor));
    }

    private sealed class SampleAutostart : IAutostartService
    {
        public bool IsSupported => true;

        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled) => IsEnabled = enabled;
    }

    private sealed class SampleLauncher : ILauncher
    {
        public void OpenUrl(Uri uri)
        {
        }

        public void OpenFolder(string path)
        {
        }
    }

    private static Bitmap ToBitmap(byte[] png)
    {
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }

    /// <summary>Generic file icons, drawn rather than taken from an operating system.</summary>
    private sealed class SampleFileIcons : IFileIconProvider
    {
        public byte[]? GetIconPng(string path, int size)
        {
            if (path.EndsWith('/') || string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                return SampleArt.Folder(size * 2);
            }

            return Path.GetExtension(path).ToUpperInvariant() switch
            {
                ".MP4" => SampleArt.Document(size * 2, SampleArt.DocumentBadge.Video),
                ".PNG" => SampleArt.Document(size * 2, SampleArt.DocumentBadge.Picture),
                _ => SampleArt.Document(size * 2, SampleArt.DocumentBadge.Lines),
            };
        }
    }
}
