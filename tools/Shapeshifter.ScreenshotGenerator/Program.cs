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
    /// <summary>Screenshot name, accent colour and scale (the PAD screenshot is a smaller rendition).</summary>
    private static readonly (string Name, string Accent, double Scale, int Width, int Height)[] Screenshots =
    [
        ("blue", "#0063B1", 1.0, 1000, 650),
        ("green", "#107C10", 1.0, 1000, 650),
        ("purple", "#881798", 1.0, 1000, 650),
        ("red", "#D41124", 1.0, 1000, 650),
        ("pad", "#0063B1", 0.73, 800, 540),
    ];

    public static int Main(string[] args)
    {
        var output = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("assets", "screenshots"));
        Directory.CreateDirectory(output);

        AppBuilder.Configure<Shapeshifter.Desktop.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

        foreach (var (name, accent, scale, width, height) in Screenshots)
        {
            var path = Path.Combine(output, name + ".PNG");
            Render(Color.Parse(accent), scale, width, height, path);
            Console.WriteLine($"Wrote {path}");
        }

        return 0;
    }

    private static void Render(Color accent, double scale, int width, int height, string path)
    {
        AccentPalette.Apply(Application.Current!, accent);

        var viewModel = new PickerViewModel { ActionsFillHeight = false, MaxListHeight = 10_000 };
        var visuals = new PickerVisuals(new SampleFileIcons(), Log.Logger);
        using var thumbnail = ToBitmap(SampleArt.Banner(900, 228));
        foreach (var item in SampleItems())
        {
            viewModel.Items.Add(new PickerItemViewModel(item, item.Kind == ClipboardContentKind.Image ? thumbnail : null, visuals));
        }

        viewModel.Actions.Add(new PickerActionView("Paste", "Pastes clipboard contents as-is."));
        viewModel.Actions.Add(new PickerActionView("Copy as file", "Copy this text to the clipboard as a file."));
        viewModel.Actions.Add(new PickerActionView("Paste as plain text", "Pastes clipboard contents as plain text with no formatting."));
        viewModel.Actions.Add(new PickerActionView(
            "Pin to clipboard",
            "Pins this item so that it always stays in the top, and gets saved to your harddrive so that it is there the next time you open Shapeshifter."));
        viewModel.SelectedIndex = 0;
        viewModel.SelectedActionIndex = 0;

        // The picker's own window cannot be captured over a desktop, so its content is moved into a window that
        // also shows a desktop background behind it.
        var picker = new PickerWindow { DataContext = viewModel };
        var content = (Control)picker.Content!;
        picker.Content = null;
        content.DataContext = viewModel;

        using var background = ToBitmap(SampleArt.Desktop(width, height));
        var host = new Window
        {
            Width = width,
            Height = height,
            WindowDecorations = WindowDecorations.None,
            Content = new Panel
            {
                Children =
                {
                    new Image { Source = background, Stretch = Stretch.Fill },
                    new LayoutTransformControl
                    {
                        LayoutTransform = new ScaleTransform(scale, scale),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = new Border { Width = picker.Width, Child = content },
                    },
                },
            },
        };

        host.Show();
        for (var i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        using var frame = host.CaptureRenderedFrame() ?? throw new InvalidOperationException("Nothing was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
        host.Close();
        picker.Close();
    }

    /// <summary>The items shown in the original screenshots.</summary>
    private static IEnumerable<ClipboardItem> SampleItems()
    {
        const string article = "Shapeshifter is a freeware clipboard manager, originally developed by defunct Flamefusion company " +
            "and later released as open source.";
        var browser = SampleArt.BrowserIcon();
        var editor = SampleArt.EditorIcon();
        const string desktop = "C:/Users/Shapeshifter/Desktop/";

        yield return new ClipboardItem(
            new ClipboardSnapshot { Text = article },
            source: new ClipboardSource("Google Chrome", "Shapeshifter (software) - Wikipedia – Google Chrome", browser));
        yield return new ClipboardItem(
            new ClipboardSnapshot { Image = SampleArt.Banner(900, 228) },
            source: new ClipboardSource("Google Chrome", "Shapeshifter (software) - Wikipedia – Google Chrome", browser));
        yield return new ClipboardItem(new ClipboardSnapshot
        {
            Files = [desktop + "Projects", desktop + "Notes.txt", desktop + "Holiday.mp4", desktop + "Sunset.png"],
        });
        yield return new ClipboardItem(new ClipboardSnapshot { Files = [desktop + "New Text Document.txt"] });
        yield return new ClipboardItem(
            new ClipboardSnapshot { Text = article + " " },
            source: new ClipboardSource("Microsoft Visual Studio", "Shapeshifter (Running) - Microsoft Visual Studio", editor));
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
                return SampleArt.Folder(size);
            }

            return Path.GetExtension(path).ToUpperInvariant() switch
            {
                ".MP4" => SampleArt.Document(size, SampleArt.DocumentBadge.Video),
                ".PNG" => SampleArt.Document(size, SampleArt.DocumentBadge.Picture),
                _ => SampleArt.Document(size, SampleArt.DocumentBadge.Lines),
            };
        }
    }
}
