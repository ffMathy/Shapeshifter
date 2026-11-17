using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Actions;
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
/// Rendering is headless (Avalonia.Headless with Skia), with the fonts installed on the machine. Each look is rendered
/// on its own operating system in CI, so that it shows in the font the app really uses there: Segoe UI on Windows, the
/// system font (SF Pro) on macOS and Adwaita Sans on Linux (<c>scripts/install-adwaita-fonts.sh</c>). Elsewhere the looks
/// fall back to the bundled Inter, with a warning. All sample artwork, including application and file icons, is drawn
/// here.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// Screenshot name, look, accent colour (null: the look's own) and display scaling. The coloured ones are used by
    /// the website (shapeshifter.app) and taken at 125 %, as the original screenshots were; the PAD one is smaller.
    /// </summary>
    private static readonly (string Name, Look Look, string? Accent, double Scale, int Width, int Height)[] Screenshots =
    [
        ("windows", Look.Windows, null, 1.25, 1400, 1000),
        ("macos", Look.MacOS, null, 1.25, 1400, 1000),
        ("linux", Look.Linux, null, 1.25, 1400, 1000),
        ("blue", Look.Windows, "#0063B1", 1.25, 1400, 1000),
        ("green", Look.Windows, "#107C10", 1.25, 1400, 1000),
        ("purple", Look.Windows, "#881798", 1.25, 1400, 1000),
        ("red", Look.Windows, "#D41124", 1.25, 1400, 1000),
        ("pad", Look.Windows, null, 0.9, 1000, 760),
    ];

    /// <summary>
    /// The font each look is shown in, by the family names that the machine's font must resolve to (any of them).
    /// </summary>
    private static readonly Dictionary<Look, (string Requested, string[] Accepted)> NativeFonts = new()
    {
        [Look.Windows] = ("Segoe UI Variable Text", ["Segoe UI"]),
        [Look.MacOS] = (".AppleSystemUIFont", ["System Font", ".AppleSystemUIFont", ".SF NS", "SF Pro"]),
        [Look.Linux] = ("Adwaita Sans", ["Adwaita Sans"]),
    };

    /// <summary>
    /// <c>[output-folder] [--look windows,macos,linux] [--native-fonts]</c>: <c>--look</c> renders only those looks'
    /// screenshots; <c>--native-fonts</c> fails, instead of warning, when a look's own font is not installed (as CI does,
    /// so that no screenshot in the wrong font is committed). Or <c>[output-folder] --live</c> to capture the real windows on the screen
    /// (see <see cref="LiveCapture"/>). Besides the pickers above, the picker with its actions chosen
    /// (<c>actions.PNG</c>) is rendered, and for each look its settings window (<c>settings-&lt;look&gt;.PNG</c>), its copy
    /// notification (<c>toast-&lt;look&gt;.PNG</c>), the same notification showing a tip (<c>tip-&lt;look&gt;.PNG</c>) and its picker with an empty history (<c>empty-&lt;look&gt;.PNG</c>). The README
    /// and the website show them. All are light; with <c>--surfaces</c> the Windows look is also rendered in dark
    /// (<c>windows-dark.PNG</c>, <c>settings-windows-dark.PNG</c>, <c>toast-windows-dark.PNG</c>, <c>empty-windows-dark.PNG</c>).
    /// </summary>
    public static int Main(string[] args)
    {
        Shapeshifter.Core.Localization.Localizer.ApplyLanguageOverride();
        if (args.Contains("--live"))
        {
            return LiveCapture.Run(args);
        }

        // Headless rendering has no window behind the panels, so no Mica, Acrylic or glass. Without this, a look rendered
        // on its own operating system (as CI does) would prepare its panels for the system's material, which makes them
        // see-through, and show the wallpaper straight through them. The looks' own panels stand in for the material.
        Environment.SetEnvironmentVariable(NativeMaterial.Variable, "off");

        var looks = ParseLooks(args);
        var strictFonts = args.Contains("--native-fonts");
        var lookIndex = Array.IndexOf(args, "--look");
        // The folder is the first argument that is neither an option nor the value after --look (when it is given:
        // without it, lookIndex + 1 is 0, which would skip a folder given first).
        var folder = args.Where((arg, index) => !arg.StartsWith("--", StringComparison.Ordinal) && (lookIndex < 0 || index != lookIndex + 1)).FirstOrDefault();
        var output = Path.GetFullPath(folder ?? Path.Combine("assets", "screenshots"));
        Directory.CreateDirectory(output);

        AppBuilder.Configure<Shapeshifter.Desktop.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

        foreach (var look in looks)
        {
            var (requested, accepted) = NativeFonts[look];
            var resolved = FontManager.Current.TryGetGlyphTypeface(new Typeface(requested), out var glyphTypeface)
                ? glyphTypeface.FamilyName
                : "(none)";
            if (!accepted.Any(name => resolved.StartsWith(name, StringComparison.OrdinalIgnoreCase)))
            {
                var message = $"The {look} look's font \"{requested}\" is not installed here (it resolves to \"{resolved}\"), " +
                    "so its screenshots would show the wrong font. Render it on its own operating system (see the Screenshots job in ci.yml).";
                if (strictFonts)
                {
                    Console.Error.WriteLine(message);
                    return 2;
                }

                Console.Error.WriteLine("Warning: " + message);
            }
            else
            {
                Console.WriteLine($"{look}: \"{requested}\" resolves to \"{resolved}\".");
            }
        }

        foreach (var (name, look, accent, scale, width, height) in Screenshots.Where(screenshot => looks.Contains(screenshot.Look)))
        {
            var path = Path.Combine(output, name + ".PNG");
            UseLook(look, accent);
            RenderPicker(look, scale, width, height, path);
            Console.WriteLine($"Wrote {path}");
        }

        // The actions of the selected item, chosen with the right arrow key, with "Paste as plain text" selected.
        if (looks.Contains(Look.Windows))
        {
            var actions = Path.Combine(output, "actions.PNG");
            UseLook(Look.Windows, null);
            RenderPicker(Look.Windows, 1.25, 1400, 1000, actions, selectedAction: 2);
            Console.WriteLine($"Wrote {actions}");

            // Searching the history (issue #458): the search bar below the header, with what matches.
            var search = Path.Combine(output, "search.PNG");
            RenderPicker(Look.Windows, 1.25, 1400, 1000, search, search: "shapeshifter");
            Console.WriteLine($"Wrote {search}");
        }

        foreach (var look in looks)
        {
            UseLook(look, null);
            var settings = Path.Combine(output, $"settings-{look.ToString().ToLowerInvariant()}.PNG");
            RenderWindow(CreateSettingsWindow(), look, settings);
            Console.WriteLine($"Wrote {settings}");
            var emptyPicker = Path.Combine(output, $"empty-{look.ToString().ToLowerInvariant()}.PNG");
            RenderPicker(look, 1.25, 1400, 820, emptyPicker, empty: true);
            Console.WriteLine($"Wrote {emptyPicker}");
            var toast = Path.Combine(output, $"toast-{look.ToString().ToLowerInvariant()}.PNG");
            var toastWindow = new ToastWindow();
            toastWindow.SetContent(5, "Shapeshifter is a freeware clipboard manager, originally developed by defunct Flamefusion company");
            RenderWindow(toastWindow, look, toast);
            Console.WriteLine($"Wrote {toast}");
            var tip = Path.Combine(output, $"tip-{look.ToString().ToLowerInvariant()}.PNG");
            RenderWindow(CreateTipWindow(look), look, tip);
            Console.WriteLine($"Wrote {tip}");
        }

        if (args.Contains("--surfaces") && looks.Contains(Look.Windows))
        {
            RenderWindowsDark(output);
        }

        return 0;
    }

    /// <summary>The notification as it shows a coaching tip (#310), in the look's modifier key.</summary>
    private static ToastWindow CreateTipWindow(Look look)
    {
        var modifier = look == Look.MacOS ? "Cmd" : "Ctrl";
        var window = new ToastWindow();
        window.SetTip(
            "Did you know?",
            $"You can copy several things from Photoshop first, then hold {modifier}+V in Paint to pick each one from your history.");
        return window;
    }

    /// <summary>The looks named by <c>--look</c> (comma-separated), or all of them.</summary>
    private static Look[] ParseLooks(string[] args)
    {
        var index = Array.IndexOf(args, "--look");
        if (index < 0 || index + 1 >= args.Length)
        {
            return Enum.GetValues<Look>();
        }

        return args[index + 1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => Enum.Parse<Look>(name, ignoreCase: true))
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// The Windows look in dark, as Windows shows it in the dark app mode: the picker, its empty state, the settings window
    /// and the copy notification (<c>&lt;name&gt;-windows-dark.PNG</c>). For review; the README and website show light only.
    /// </summary>
    private static void RenderWindowsDark(string output)
    {
        UseLook(Look.Windows, null, ThemeVariant.Dark);
        var picker = Path.Combine(output, "windows-dark.PNG");
        RenderPicker(Look.Windows, 1.25, 1400, 1000, picker, dark: true);
        Console.WriteLine($"Wrote {picker}");
        var settings = Path.Combine(output, "settings-windows-dark.PNG");
        RenderWindow(CreateSettingsWindow(), Look.Windows, settings, dark: true);
        Console.WriteLine($"Wrote {settings}");
        var emptyPicker = Path.Combine(output, "empty-windows-dark.PNG");
        RenderPicker(Look.Windows, 1.25, 1400, 820, emptyPicker, empty: true, dark: true);
        Console.WriteLine($"Wrote {emptyPicker}");
        var toast = Path.Combine(output, "toast-windows-dark.PNG");
        var toastWindow = new ToastWindow();
        toastWindow.SetContent(5, "Shapeshifter is a freeware clipboard manager, originally developed by defunct Flamefusion company");
        RenderWindow(toastWindow, Look.Windows, toast, dark: true);
        Console.WriteLine($"Wrote {toast}");
        UseLook(Look.Windows, null);
    }

    /// <summary>Applies <paramref name="look"/> in light (or <paramref name="theme"/>), whatever the machine's own theme.</summary>
    private static void UseLook(Look look, string? accent, ThemeVariant? theme = null)
    {
        var application = Application.Current!;
        PlatformLook.Apply(application, look, theme ?? ThemeVariant.Light);
        var color = accent ?? PlatformLook.Resource(application, "LookDefaultAccent", "#0067C0");
        AccentPalette.Apply(application, Color.Parse(color));
    }

    /// <summary>
    /// The actions the app offers for <paramref name="item"/>, as the engine lists them (the real action catalog), so the
    /// screenshots show the right titles: "Unpin from clipboard" for the pinned sample item, for instance.
    /// </summary>
    internal static IReadOnlyList<PickerActionView> SampleActions(ClipboardItem? item) => item is null
        ? []
        : ActionCatalog.CreateDefault().GetAvailable(item).Select(action => new PickerActionView(action.Id, action.GetTitle(item), action.GetDescription(item))).ToArray();

    /// <summary>The picker's state in the screenshots: the sample items, the first selected, its actions.</summary>
    internal static PickerViewModel CreateSampleViewModel(Look look, byte[] photo, Bitmap thumbnail)
    {
        var viewModel = new PickerViewModel(look == Look.MacOS ? "Cmd" : "Ctrl") { MaxListHeight = 10_000 };
        var visuals = new PickerVisuals(new SampleFileIcons(), Log.Logger);
        var items = SampleItems(look, photo).ToArray();
        foreach (var item in items)
        {
            viewModel.Items.Add(new PickerItemViewModel(item, item.Kind == ClipboardContentKind.Image ? thumbnail : null, visuals));
        }

        var actions = SampleActions(items.FirstOrDefault());
        for (var i = 0; i < actions.Count; i++)
        {
            viewModel.Actions.Add(new PickerActionViewModel(actions[i], isDefault: i == 0));
        }

        viewModel.SelectedIndex = 0;
        viewModel.SelectedActionIndex = 0;
        viewModel.UpdateActionsHeader();
        return viewModel;
    }

    /// <param name="selectedAction">When set, the actions pane is active with this action selected.</param>
    /// <param name="search">When set, the picker is searching for this, showing only the sample items that match.</param>
    private static void RenderPicker(
        Look look, double scale, int width, int height, string path, bool empty = false, int? selectedAction = null, bool dark = false, string? search = null)
    {
        var photo = SampleArt.Banner(900, 600);
        using var thumbnail = ToBitmap(photo);
        var viewModel = empty
            ? new PickerViewModel(look == Look.MacOS ? "Cmd" : "Ctrl") { MaxListHeight = 10_000 }
            : CreateSampleViewModel(look, photo, thumbnail);
        viewModel.IsOpaque = false;
        if (search is not null)
        {
            var terms = HistorySearch.ParseQuery(search);
            foreach (var item in viewModel.Items.Where(item => !HistorySearch.Matches(HistorySearch.Normalize($"{item.Title}\n{item.SourceText}"), terms)).ToArray())
            {
                viewModel.Items.Remove(item);
            }

            viewModel.IsSearching = true;
            viewModel.SearchQuery = search;
        }

        if (selectedAction is { } action)
        {
            viewModel.IsActionsPaneActive = true;
            viewModel.SelectedActionIndex = action;
        }

        // The picker's own window cannot be captured over a desktop, so its content is moved into a window that
        // also shows a desktop background behind it.
        var picker = new PickerWindow { DataContext = viewModel };
        var content = (Control)picker.Content!;
        picker.Content = null;
        content.DataContext = viewModel;
        content.FlowDirection = picker.FlowDirection;

        using var background = ToBitmap(SampleArt.Desktop(look, width, height, dark));
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
    private static void RenderWindow(Window window, Look look, string path, bool dark = false)
    {
        var content = (Control)window.Content!;
        window.Content = null;
        content.DataContext = window.DataContext;
        content.FlowDirection = window.FlowDirection;
        if (!double.IsNaN(window.Width))
            content.Width = window.Width - (content.Margin.Left + content.Margin.Right);
        var host = new Window
        {
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowDecorations = WindowDecorations.None,
            FontFamily = window.FontFamily,
            Background = new SolidColorBrush(Color.Parse(look switch { Look.MacOS => "#DCDDE6", Look.Linux => "#DEDDDA", _ when dark => "#1A2333", _ => "#D8E2F0" })),
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

    internal static SettingsWindow CreateSettingsWindow()
    {
        var root = Path.Combine(Path.GetTempPath(), "shapeshifter-screenshots");
        var paths = AppPaths.ForRoot(root);
        var store = new SettingsStore(Path.Combine(root, "settings.json"));
        return new SettingsWindow
        {
            DataContext = new SettingsViewModel(
                store, new SampleAutostart(), new ClipboardHistory(25), new SampleLauncher(), paths,
                () => PlatformLook.Current == Look.MacOS ? "Cmd" : "Ctrl", [], shortcutModifierNames: PlatformLook.Current switch
                {
                    Look.Windows => ["Ctrl", "Win"],
                    Look.MacOS => ["Cmd", "Ctrl"],
                    _ => ["Ctrl"],
                }),
        };
    }

    /// <summary>
    /// The items shown in the screenshots, copied a few minutes apart from applications typical of the platform.
    /// </summary>
    internal static IEnumerable<ClipboardItem> SampleItems(Look look, byte[] photo)
    {
        var now = DateTimeOffset.Now;
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

        // The first item is pinned, as pinned items are kept at the top. All are copied within the hour, so the times
        // read the same whenever this runs.
        var pinned = new ClipboardItem(
            new ClipboardSnapshot
            {
                Text = article,

                // Copied from a browser, which also puts HTML on the clipboard: so "Paste as plain text" is offered.
                RawFormats = [new RawClipboardFormat(look == Look.Windows ? "HTML Format" : "text/html", System.Text.Encoding.UTF8.GetBytes($"<p>{article}</p>"))],
            },
            capturedAt: now - TimeSpan.FromMinutes(47),
            source: new ClipboardSource(browserName, page, browser));
        new ClipboardHistory().AddPinned(pinned);
        yield return pinned;
        yield return new ClipboardItem(
            new ClipboardSnapshot { Image = photo }, capturedAt: now, source: new ClipboardSource(browserName, page, browser));
        yield return new ClipboardItem(
            new ClipboardSnapshot { Files = [desktop + "Projects", desktop + "Notes.txt", desktop + "Holiday.mp4", desktop + "Sunset.png"] },
            capturedAt: now - TimeSpan.FromMinutes(2));
        yield return new ClipboardItem(new ClipboardSnapshot { Files = [desktop + newFile] }, capturedAt: now - TimeSpan.FromMinutes(9));
        yield return new ClipboardItem(
            new ClipboardSnapshot { Text = "dotnet run --project src/Shapeshifter.Desktop" },
            capturedAt: now - TimeSpan.FromMinutes(25),
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

    internal static Bitmap ToBitmap(byte[] png)
    {
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }

    /// <summary>Generic file icons, drawn rather than taken from an operating system.</summary>
    internal sealed class SampleFileIcons : IFileIconProvider
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
