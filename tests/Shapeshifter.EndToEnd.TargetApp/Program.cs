using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Shapeshifter.EndToEnd.TargetApp;

/// <summary>
/// A separate process with a text box. Commands arrive on stdin, one per line; every command is answered
/// with one line on stdout. Arguments are base64-encoded UTF-8.
/// <list type="bullet">
/// <item><c>copy-text &lt;text&gt;</c>: put plain text on the clipboard.</item>
/// <item><c>copy-html &lt;text&gt; &lt;html&gt;</c>: put text and HTML (the platform's native HTML format) on the clipboard.</item>
/// <item><c>copy-secret &lt;text&gt;</c>: put text marked as a password-manager secret on the clipboard.</item>
/// <item>
/// <c>copy-image &lt;scenario&gt;</c> (Windows): put <see cref="TestImage"/> on the clipboard exactly as a real
/// application does. <c>screenshot</c>: CF_DIB (24-bit) and PNG, like Snipping Tool. <c>firefox</c>: CF_DIB and a
/// CF_HDROP pointing to a temporary copy of the image, like Firefox's "Copy Image".
/// </item>
/// <item>
/// <c>copy-bitmap</c>: put <see cref="TestImage"/> on the clipboard as a bitmap, through Avalonia (every platform).
/// </item>
/// <item>
/// <c>read-image</c>: read the clipboard's image the way an application pasting it does; answers
/// <c>IMAGE &lt;width&gt;x&lt;height&gt;</c>, or <c>NONE</c>.
/// </item>
/// <item><c>delete-temp</c>: delete the temporary file of the <c>firefox</c> scenario, as Firefox does later.</item>
/// <item><c>describe-image</c> (Windows): answers <c>IMAGE &lt;description&gt;</c>, see <see cref="WindowsClipboard.Describe"/>.</item>
/// <item>
/// With <c>--mirror &lt;file&gt;</c> the application takes no commands; it keeps its window in front, writes
/// <c>&lt;file&gt;.ready</c> once it is shown and mirrors the text box to <c>&lt;file&gt;</c> on every change. This
/// is how a test reads what arrives in a copy running inside a Remote Desktop session.
/// </item>
/// <item><c>focus</c>, <c>clear</c>, <c>get</c> (answers <c>TEXT &lt;text&gt;</c>), <c>formats</c> (answers <c>FORMATS a|b</c>), <c>quit</c>.</item>
/// </list>
/// </summary>
internal sealed class TargetApplication : Application
{
    private Window? window;
    private TextBox? textBox;

    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        textBox = new TextBox { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        window = new Window
        {
            Title = "Shapeshifter E2E Target",
            Width = 500,
            Height = 300,
            Position = new PixelPoint(40, 40),
            Content = textBox,
        };
        lifetime.MainWindow = window;
        var mirror = MirrorPath(lifetime.Args ?? []);
        if (mirror is not null)
        {
            StartMirroring(mirror);
            base.OnFrameworkInitializationCompleted();
            return;
        }

        window.Opened += (_, _) =>
        {
            Focus();
            Console.WriteLine("TARGET_READY");
            new Thread(ReadCommands) { IsBackground = true }.Start();
        };
        base.OnFrameworkInitializationCompleted();
    }

    private static string? MirrorPath(string[] args)
    {
        var index = Array.IndexOf(args, "--mirror");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private void StartMirroring(string path)
    {
        window!.WindowState = WindowState.Maximized;
        window.Topmost = true;
        textBox!.TextChanged += (_, _) => File.WriteAllText(path, textBox.Text ?? string.Empty);
        window.Opened += (_, _) =>
        {
            Focus();
            File.WriteAllText(path, string.Empty);
            File.WriteAllText(path + ".ready", Environment.UserName);
        };

        // Nothing else should take the focus in the remote session, but a freshly created profile can show
        // first-logon windows, so take it back now and then.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            if (!window.IsActive)
            {
                Focus();
            }
        };
        timer.Start();
    }

    private void ReadCommands()
    {
        while (Console.ReadLine() is { } line)
        {
            string response;
            try
            {
                response = Dispatcher.UIThread.InvokeAsync(() => ExecuteAsync(line)).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                response = "ERROR " + exception.GetType().Name + ": " + exception.Message.ReplaceLineEndings(" ");
            }

            Console.WriteLine(response);
            if (line == "quit")
            {
                break;
            }
        }

        Dispatcher.UIThread.Post(() => ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Shutdown());
    }

    private async Task<string> ExecuteAsync(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var clipboard = window!.Clipboard!;
        switch (parts[0])
        {
            case "copy-text":
                await clipboard.SetTextAsync(Decode(parts[1]));
                return "OK";

            case "copy-html":
                var item = new DataTransferItem();
                item.SetText(Decode(parts[1]));
                item.Set(DataFormat.CreateBytesPlatformFormat(HtmlFormatName), EncodeHtml(Decode(parts[2])));
                var transfer = new DataTransfer();
                transfer.Add(item);
                await clipboard.SetDataAsync(transfer);
                return "OK";

            case "copy-secret":
                // What password managers do: the text plus the platform's "do not record this" marker.
                var secret = new DataTransferItem();
                secret.SetText(Decode(parts[1]));
                secret.Set(DataFormat.CreateBytesPlatformFormat(SecretMarkerFormat), SecretMarkerValue);
                var secretTransfer = new DataTransfer();
                secretTransfer.Add(secret);
                await clipboard.SetDataAsync(secretTransfer);
                return "OK";

            case "copy-bitmap":
                using (var stream = new MemoryStream(EncodePng()))
                {
                    var bitmapItem = new DataTransferItem();
                    bitmapItem.SetBitmap(new Bitmap(stream));
                    var bitmapTransfer = new DataTransfer();
                    bitmapTransfer.Add(bitmapItem);
                    await clipboard.SetDataAsync(bitmapTransfer);
                }

                return "OK";

            case "read-image":
                using (var data = await clipboard.TryGetDataAsync())
                {
                    using var image = data is null ? null : await data.TryGetBitmapAsync();
                    return image is null ? "NONE" : $"IMAGE {image.PixelSize.Width}x{image.PixelSize.Height}";
                }

            case "copy-image" when OperatingSystem.IsWindows():
                CopyImage(parts[1]);
                return "OK";

            case "delete-temp":
                if (temporaryImage is not null)
                {
                    File.Delete(temporaryImage);
                }

                return "OK";

            case "describe-image" when OperatingSystem.IsWindows():
                return "IMAGE " + WindowsClipboard.Describe(WindowHandle, DescribePng);

            case "focus":
                Focus();
                return "OK";

            case "clear":
                textBox!.Text = string.Empty;
                return "OK";

            case "get":
                return "TEXT " + Convert.ToBase64String(Encoding.UTF8.GetBytes(textBox!.Text ?? string.Empty));

            case "formats":
                using (var data = await clipboard.TryGetDataAsync())
                {
                    return "FORMATS " + (data is null ? string.Empty : string.Join('|', data.Formats.Select(f => f.Identifier)));
                }

            case "quit":
                return "OK";

            default:
                return "ERROR unknown command";
        }
    }

    /// <summary>A 4 × 3 opaque image with distinct colours, as ARGB, rows top to bottom.</summary>
    public static readonly uint[] TestImage =
    [
        0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFFFF,
        0xFF000000, 0xFFFFFF00, 0xFF00FFFF, 0xFFFF00FF,
        0xFF808080, 0xFFFF8000, 0xFF123456, 0xFFFEDCBA,
    ];

    private const int TestImageWidth = 4;
    private const int TestImageHeight = 3;

    private string? temporaryImage;

    private IntPtr WindowHandle => window!.TryGetPlatformHandle()?.Handle ?? throw new InvalidOperationException("No window handle");

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void CopyImage(string scenario)
    {
        var dib = WindowsClipboard.CreateDib24(TestImageWidth, TestImageHeight, TestImage);
        switch (scenario)
        {
            case "screenshot":
                WindowsClipboard.Set(WindowHandle, [(WindowsClipboard.CfDib, dib), (WindowsClipboard.PngFormat, EncodePng())]);
                break;

            case "firefox":
                temporaryImage = Path.Combine(Path.GetTempPath(), "shapeshifter-e2e-" + Guid.NewGuid().ToString("N")[..8] + ".png");
                File.WriteAllBytes(temporaryImage, EncodePng());
                WindowsClipboard.Set(WindowHandle, [(WindowsClipboard.CfDib, dib), (WindowsClipboard.CfHdrop, WindowsClipboard.CreateHdrop(temporaryImage))]);
                break;

            default:
                throw new ArgumentException("Unknown scenario " + scenario);
        }
    }

    private static byte[] EncodePng()
    {
        using var bitmap = new WriteableBitmap(new PixelSize(TestImageWidth, TestImageHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < TestImageHeight; y++)
            {
                var row = new byte[TestImageWidth * 4];
                for (var x = 0; x < TestImageWidth; x++)
                {
                    var pixel = TestImage[y * TestImageWidth + x];
                    row[x * 4] = (byte)pixel;
                    row[x * 4 + 1] = (byte)(pixel >> 8);
                    row[x * 4 + 2] = (byte)(pixel >> 16);
                    row[x * 4 + 3] = (byte)(pixel >> 24);
                }

                System.Runtime.InteropServices.Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    /// <summary>Same shape as <see cref="WindowsClipboard.DescribeDib"/>, so the two can be compared directly.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string DescribePng(byte[] png)
    {
        using var bitmap = WriteableBitmap.Decode(new MemoryStream(png));
        using var buffer = bitmap.Lock();
        if (buffer.Format != PixelFormat.Bgra8888)
        {
            return $"{buffer.Size.Width}x{buffer.Size.Height} format={buffer.Format} pixels=unsupported";
        }

        var pixels = new List<string>();
        var row = new byte[buffer.Size.Width * 4];
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
            for (var x = 0; x < buffer.Size.Width; x++)
            {
                pixels.Add(WindowsClipboard.FormatPixel(row[x * 4 + 3], row[x * 4 + 2], row[x * 4 + 1], row[x * 4]));
            }
        }

        return $"{buffer.Size.Width}x{buffer.Size.Height} pixels={string.Join(',', pixels)}";
    }

    private void Focus()
    {
        window!.Activate();
        textBox!.Focus();
        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }

    private static string SecretMarkerFormat =>
        OperatingSystem.IsWindows() ? "ExcludeClipboardContentFromMonitorProcessing"
        : OperatingSystem.IsMacOS() ? "org.nspasteboard.ConcealedType"
        : "x-kde-passwordManagerHint";

    private static byte[] SecretMarkerValue => OperatingSystem.IsLinux() ? Encoding.UTF8.GetBytes("secret") : [0];

    private static string HtmlFormatName =>
        OperatingSystem.IsWindows() ? "HTML Format" : OperatingSystem.IsMacOS() ? "public.html" : "text/html";

    /// <summary>Windows expects the CF_HTML header with byte offsets; the other platforms take plain UTF-8 HTML.</summary>
    private static byte[] EncodeHtml(string fragment)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Encoding.UTF8.GetBytes(fragment);
        }

        const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        var prefix = "<html><body><!--StartFragment-->";
        var suffix = "<!--EndFragment--></body></html>";
        var headerLength = string.Format(System.Globalization.CultureInfo.InvariantCulture, header, 0, 0, 0, 0).Length;
        var startHtml = headerLength;
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(suffix);
        var text = string.Format(System.Globalization.CultureInfo.InvariantCulture, header, startHtml, endHtml, startFragment, endFragment)
            + prefix + fragment + suffix;
        return Encoding.UTF8.GetBytes(text);
    }

    private static string Decode(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) =>
        AppBuilder.Configure<TargetApplication>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
