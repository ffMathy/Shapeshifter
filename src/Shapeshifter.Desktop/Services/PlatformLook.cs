using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Shapeshifter.Desktop.Services;

/// <summary>The visual identity Shapeshifter wears, matched to the design language of the operating system.</summary>
public enum Look
{
    /// <summary>Windows 11, Fluent 2: an Acrylic flyout ("Mica Sheets").</summary>
    Windows,

    /// <summary>macOS 26: a floating sheet of Liquid Glass ("Refraction").</summary>
    MacOS,

    /// <summary>Linux, GNOME libadwaita on every desktop ("Boxed Sheet").</summary>
    Linux,
}

/// <summary>
/// Chooses and applies the <see cref="Look"/>: each look is a style file (<c>Styles/&lt;Look&gt;.axaml</c>) over the shared
/// layout of the windows, and a folder of icons (<c>Assets/&lt;Look&gt;</c>).
/// </summary>
public static class PlatformLook
{
    /// <summary>Overrides the look, for development and screenshots: <c>windows</c>, <c>macos</c> or <c>linux</c>.</summary>
    public const string Variable = "SHAPESHIFTER_LOOK";

    /// <summary>
    /// Overrides the theme of the Windows look, for development and screenshots: <c>light</c> or <c>dark</c>. Without it the
    /// Windows look follows the app mode of Windows (Settings, Personalisation, Colours), live.
    /// </summary>
    public const string ThemeVariable = "SHAPESHIFTER_THEME";

    private static StyleInclude? applied;

    /// <summary>The look that is applied, once <see cref="Apply(Application)"/> has run.</summary>
    public static Look Current { get; private set; } = Detect();

    /// <summary>The look of the operating system Shapeshifter runs on, unless <see cref="Variable"/> names another.</summary>
    public static Look Detect()
    {
        if (Enum.TryParse<Look>(Environment.GetEnvironmentVariable(Variable), ignoreCase: true, out var chosen))
        {
            return chosen;
        }

        if (OperatingSystem.IsWindows())
        {
            return Look.Windows;
        }

        return OperatingSystem.IsMacOS() ? Look.MacOS : Look.Linux;
    }

    /// <summary>Applies the detected look.</summary>
    public static void Apply(Application application) => Apply(application, Detect());

    /// <summary>Applies <paramref name="look"/>, replacing the look applied before.</summary>
    public static void Apply(Application application, Look look) => Apply(application, look, null);

    /// <summary>
    /// Applies <paramref name="look"/>, replacing the look applied before, in <paramref name="theme"/>. Only the Windows look
    /// has a dark variant: without a theme it follows the system's (<see cref="ThemeVariant.Default"/>, which Avalonia
    /// updates live from the app mode of Windows), unless <see cref="ThemeVariable"/> names one. The macOS and Linux looks
    /// are always light.
    /// </summary>
    public static void Apply(Application application, Look look, ThemeVariant? theme)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (applied is not null)
        {
            application.Styles.Remove(applied);
        }

        applied = new StyleInclude(new Uri("avares://Shapeshifter/")) { Source = new Uri($"avares://Shapeshifter/Styles/{look}.axaml") };
        application.Styles.Add(applied);
        application.RequestedThemeVariant = look == Look.Windows ? theme ?? RequestedTheme() : ThemeVariant.Light;
        Current = look;
    }

    /// <summary>The theme <see cref="ThemeVariable"/> asks for, or <see cref="ThemeVariant.Default"/>: the system's.</summary>
    private static ThemeVariant RequestedTheme() => Environment.GetEnvironmentVariable(ThemeVariable)?.Trim().ToUpperInvariant() switch
    {
        "LIGHT" => ThemeVariant.Light,
        "DARK" => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    /// <summary>The application icon of the current look, as shown in the windows' headers.</summary>
    public static Avalonia.Media.Imaging.Bitmap ApplicationIcon
    {
        get
        {
            using var stream = Avalonia.Platform.AssetLoader.Open(Asset("Application.png"));
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }

    /// <summary>An icon of the current look, such as <c>Application.png</c> or <c>Tray.png</c>.</summary>
    public static Uri Asset(string name) => Asset(Current, name);

    /// <summary>An icon of <paramref name="look"/>.</summary>
    public static Uri Asset(Look look, string name) => new($"avares://Shapeshifter/Assets/{look}/{name}");

    /// <summary>A resource of the current look, such as a row height, or <paramref name="fallback"/>.</summary>
    public static T Resource<T>(Application application, string key, T fallback)
    {
        ArgumentNullException.ThrowIfNull(application);
        return application.TryGetResource(key, application.ActualThemeVariant, out var value) && value is T typed ? typed : fallback;
    }
}
