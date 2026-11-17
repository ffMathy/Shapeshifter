using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Desktop.Views;

namespace Shapeshifter.ScreenshotGenerator;

/// <summary>
/// Shows the real windows on the real screen, through the operating system's own windowing (blur, translucency,
/// system fonts and the system accent colour, none of which the headless renderer has), and captures the screen.
/// </summary>
/// <remarks>
/// Run on a desktop session, such as a macOS or Windows GitHub Actions runner (<c>.github/workflows/live-screenshots.yml</c>):
/// <c>dotnet run --project tools/Shapeshifter.ScreenshotGenerator -- &lt;output-folder&gt; --live</c>. The picker and the
/// copy notification are shown by the app's own <see cref="PickerPresenter"/> and <see cref="CopyNotifier"/>, so their
/// placement is captured too. Set <c>SHAPESHIFTER_NATIVE_MATERIAL</c> (see <see cref="NativeMaterial"/>) to capture the
/// windows without the system's material, or with Liquid Glass on macOS. Captures use <c>screencapture</c> on macOS,
/// PowerShell on Windows and <c>import</c> (ImageMagick) elsewhere.
/// <c>diagnostics.txt</c> records what was resolved at run time: the system's and the app's theme variant, the
/// transparency level and backdrop of each window, the fonts, the accent colour and the screen.
/// </remarks>
internal static class LiveCapture
{
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(1.5);

    private static string output = string.Empty;
    private static readonly StringBuilder Diagnostics = new();
    private static int exitCode;

    public static int Run(string[] args)
    {
        var folder = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        output = Path.GetFullPath(folder ?? "live-screenshots");
        Directory.CreateDirectory(output);
        Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateLogger();

        AppBuilder.Configure<LiveApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new MacOSPlatformOptions { ShowInDock = false })
            .StartWithClassicDesktopLifetime([], ShutdownMode.OnExplicitShutdown);
        return exitCode;
    }

    private sealed class LiveApp : Application
    {
        public override void Initialize() => Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

        public override void OnFrameworkInitializationCompleted()
        {
            PlatformLook.Apply(this);
            AccentPalette.Attach(this);
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await CaptureAllAsync().ConfigureAwait(true);
                    }
                    catch (Exception exception)
                    {
                        Console.Error.WriteLine(exception);
                        Diagnostics.AppendLine(CultureInfo.InvariantCulture, $"FAILED: {exception}");
                        exitCode = 1;
                    }
                    finally
                    {
                        await File.WriteAllTextAsync(Path.Combine(output, "diagnostics.txt"), Diagnostics.ToString()).ConfigureAwait(true);
                        desktop.Shutdown(exitCode);
                    }
                });
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    private static async Task CaptureAllAsync()
    {
        var application = Application.Current!;
        var look = PlatformLook.Current;
        Note("Look", look.ToString());
        Note("OS", Environment.OSVersion.VersionString);
        var colors = application.PlatformSettings?.GetColorValues();
        Note("System accent", colors is null ? "(none)" : colors.AccentColor1.ToString());
        Note("System theme", colors is null ? "(none)" : colors.ThemeVariant.ToString());
        Note("App theme", $"requested {application.RequestedThemeVariant?.ToString() ?? "(none)"}, actual {application.ActualThemeVariant}");
        if (OperatingSystem.IsWindows())
        {
            Note("Transparency effects", Shapeshifter.Platform.Windows.WindowsWindowMaterial.AreTransparencyEffectsEnabled ? "on" : "off");
        }
        Note("Default font", $"\"{FontManager.Current.DefaultFontFamily}\"");
        foreach (var family in new[] { ".AppleSystemUIFont", "SF Pro Text", "SF Pro", "Segoe UI Variable Text", "Adwaita Sans", "Cantarell" })
        {
            Note("Font lookup", FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var found)
                ? $"\"{family}\" -> \"{found.FamilyName}\""
                : $"\"{family}\" -> (not found)");
        }


        // A document window behind the left half of the picker, so the capture shows the material over a busy
        // window as well as over the wallpaper.
        var backdrop = CreateBackdrop();
        backdrop.Show();
        var screen = backdrop.Screens.Primary;
        if (screen is not null)
        {
            Note("Screen", $"bounds {screen.Bounds}, working area {screen.WorkingArea}, scaling {screen.Scaling}");
            Note("Desktop scaling", backdrop.DesktopScaling.ToString(CultureInfo.InvariantCulture));
            var scaling = OperatingSystem.IsMacOS() ? backdrop.DesktopScaling : screen.Scaling;
            var area = screen.WorkingArea;
            backdrop.Position = new PixelPoint(area.X, area.Y + (int)(area.Height * 0.1));
            backdrop.Width = area.Width / scaling / 2;
            backdrop.Height = area.Height / scaling * 0.8;
        }

        var presenter = new PickerPresenter(new NoCursor(), new PickerVisuals(new Program.SampleFileIcons(), Log.Logger), look == Look.MacOS ? "Cmd" : "Ctrl", Log.Logger);
        var items = Program.SampleItems(look, SampleArt.Banner(900, 600)).ToArray();
        var actions = Program.SampleActions(items[0]);

        await presenter.ShowAsync(new PickerViewState(items, 0, actions, 0, PickerPane.Items)).ConfigureAwait(true);
        await Task.Delay(Settle).ConfigureAwait(true);
        DescribeOpenWindows("Picker");
        await CaptureAsync("picker").ConfigureAwait(true);

        await presenter.UpdateAsync(new PickerViewState(items, 1, actions, 0, PickerPane.Items)).ConfigureAwait(true);
        await Task.Delay(Settle).ConfigureAwait(true);
        await CaptureAsync("picker-image-selected").ConfigureAwait(true);

        await presenter.UpdateAsync(new PickerViewState(items, 0, actions, 2, PickerPane.Actions)).ConfigureAwait(true);
        await Task.Delay(Settle).ConfigureAwait(true);
        await CaptureAsync("picker-actions").ConfigureAwait(true);

        await presenter.HideAsync().ConfigureAwait(true);

        var settings = Program.CreateSettingsWindow();
        settings.Show();
        await Task.Delay(Settle).ConfigureAwait(true);
        DescribeOpenWindows("Settings");
        await CaptureAsync("settings").ConfigureAwait(true);
        settings.Close();

        var store = new SettingsStore(Path.Combine(Path.GetTempPath(), "shapeshifter-live", "settings.json"));
        new CopyNotifier(store).NotifyCopied(items[0], items.Length);
        await Task.Delay(TimeSpan.FromSeconds(0.6)).ConfigureAwait(true);
        DescribeOpenWindows("Notification");
        await CaptureAsync("notification").ConfigureAwait(true);

        backdrop.Close();
    }

    private static Window CreateBackdrop()
    {
        var lines = new StackPanel { Spacing = 10, Margin = new Thickness(32) };
        lines.Children.Add(new TextBlock { Text = "Reading list", FontSize = 28, FontWeight = FontWeight.SemiBold });
        string[] colors = ["#E8505B", "#F9A03F", "#2BB673", "#3A86FF", "#8338EC"];
        for (var i = 0; i < 14; i++)
        {
            lines.Children.Add(new Border
            {
                Height = 14,
                Width = 180 + (i * 37 % 260),
                CornerRadius = new CornerRadius(7),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.Parse(colors[i % colors.Length])),
            });
        }

        return new Window
        {
            Title = "Notes",
            Width = 700,
            Height = 600,
            Background = Brushes.White,
            Content = lines,
            ShowActivated = false,
        };
    }

    private static void DescribeOpenWindows(string label)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows.Where(window => window.IsVisible && window is PickerWindow or SettingsWindow or ToastWindow))
        {
            var font = window.FontFamily;
            var resolved = FontManager.Current.TryGetGlyphTypeface(new Typeface(font), out var glyphTypeface)
                ? glyphTypeface.FamilyName
                : "(not resolved)";
            Note(label, $"{window.GetType().Name}: position {window.Position}, size {window.Bounds.Size}, " +
                $"theme {window.ActualThemeVariant}, " +
                $"transparency {window.ActualTransparencyLevel} (asked {string.Join(", ", window.TransparencyLevelHint)}), " +
                $"font \"{font}\" resolves to \"{resolved}\", " +
                $"system material {(UsesNativeMaterial(window) ? "on" : "off")}, " +
                $"backdrop {Backdrop(window)}");
        }
    }

    /// <summary>The settings window's Mica: the system's (DWM, under the title bar too), Avalonia's, or none.</summary>
    private static string Backdrop(Window window) =>
        window.Classes.Contains(NativeMaterial.SystemMicaClassName) ? "system Mica"
        : window.Classes.Contains(NativeMaterial.MicaClassName) ? "Avalonia Mica"
        : "none";

    private static bool UsesNativeMaterial(Window window) =>
        window.GetVisualDescendants().OfType<Border>().Any(border => border.Classes.Contains(NativeMaterial.ClassName));

    private static async Task CaptureAsync(string name)
    {
        var path = Path.Combine(output, name + ".png");
        var (tool, arguments) = OperatingSystem.IsMacOS() ? ("screencapture", new[] { "-x", path })
            : OperatingSystem.IsWindows() ? ("powershell", new[] { "-NoProfile", "-NonInteractive", "-Command", WindowsCapture(path) })
            : ("import", new[] { "-window", "root", path });
        using var process = Process.Start(new ProcessStartInfo(tool, arguments) { RedirectStandardError = true })
            ?? throw new InvalidOperationException($"Could not start {tool}.");
        var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(true);
        await process.WaitForExitAsync().ConfigureAwait(true);
        if (process.ExitCode != 0 || !File.Exists(path))
        {
            throw new InvalidOperationException($"{tool} failed ({process.ExitCode}): {error}");
        }

        Note("Captured", path);
    }

    /// <summary>A PowerShell command that saves the whole virtual screen (all monitors) as a PNG.</summary>
    private static string WindowsCapture(string path) =>
        "Add-Type -AssemblyName System.Windows.Forms, System.Drawing; " +
        "$b = [System.Windows.Forms.SystemInformation]::VirtualScreen; " +
        "$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height; " +
        "$g = [System.Drawing.Graphics]::FromImage($bmp); " +
        "$g.CopyFromScreen($b.Left, $b.Top, 0, 0, $bmp.Size); " +
        $"$bmp.Save('{path.Replace("'", "''", StringComparison.Ordinal)}', [System.Drawing.Imaging.ImageFormat]::Png)";

    private static void Note(string label, string text)
    {
        var line = $"{label}: {text}";
        Diagnostics.AppendLine(line);
        Console.WriteLine(line);
    }

    /// <summary>No pointer position, so the picker opens on the primary screen.</summary>
    private sealed class NoCursor : ICursorLocator
    {
        public (int X, int Y)? GetCursorPosition() => null;
    }
}
