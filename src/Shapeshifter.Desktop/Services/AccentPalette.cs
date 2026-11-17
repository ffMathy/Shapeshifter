using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Shapeshifter.Platform.Windows;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Colours the picker with the operating system's accent colour, as the original Shapeshifter did, and follows it
/// when the user changes it. Where the platform reports no accent colour the palette in Theme.axaml stays.
/// </summary>
/// <remarks>
/// The shade of the accent used for text, strokes and the toggle (<c>AccentTextBrush</c>) depends on the theme: darkened
/// until it reads on white in light, lightened until it reads on the dark surfaces in dark (only the Windows look has a
/// dark variant). It therefore goes into the theme dictionaries of the application's resources, never at their top
/// level: a top-level entry would win over every theme dictionary.
/// </remarks>
public static class AccentPalette
{
    private const string TextBrushKey = "AccentTextBrush";
    private const string TextColorKey = "AccentTextColor";

    /// <summary>The darkest surface accent text is drawn on in dark: Fluent's dark flyout tint.</summary>
    private static readonly Color DarkSurface = Color.FromRgb(0x2C, 0x2C, 0x2C);

    /// <summary>The lightest dark surface accent strokes are drawn on: the selected action over a white desktop.</summary>
    private static readonly Color DarkSurfaceOverLight = Color.FromRgb(0x57, 0x57, 0x57);

    public static void Attach(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        var settings = application.PlatformSettings;
        if (settings is null)
        {
            return;
        }

        Apply(application, settings.GetColorValues());
        settings.ColorValuesChanged += (_, values) => Apply(application, values);
    }

    internal static void Apply(Application application, PlatformColorValues values)
    {
        if (values.AccentColor1.A != 0)
        {
            Apply(application, values.AccentColor1);
        }
    }

    /// <summary>Colours the picker with <paramref name="accent"/> (its alpha is ignored).</summary>
    public static void Apply(Application application, Color accent)
    {
        ArgumentNullException.ThrowIfNull(application);
        accent = Color.FromRgb(accent.R, accent.G, accent.B);
        var dark = Darken(accent, 0.7);
        application.Resources["AccentBrush"] = new SolidColorBrush(accent);
        application.Resources["AccentDarkBrush"] = new SolidColorBrush(dark);
        application.Resources["AccentDarkerBrush"] = new SolidColorBrush(Darken(accent, 0.6));

        // Slightly see-through, so the desktop shows faintly behind the actions as in the original design.
        application.Resources["ActionsBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0xF0, dark.R, dark.G, dark.B));

        // For the looks: a faint tint for selected rows, and the accent in a shade that reads as text and strokes in each
        // theme (and as a fill under white text in light).
        application.Resources["AccentSoftBrush"] = new SolidColorBrush(Color.FromArgb(0x24, accent.R, accent.G, accent.B));
        application.Resources["AccentFaintBrush"] = new SolidColorBrush(Color.FromArgb(0x14, accent.R, accent.G, accent.B));
        application.Resources.Remove(TextBrushKey);
        application.Resources.Remove(TextColorKey);
        SetThemed(application, ThemeVariant.Default, LightThemeAccentText(accent));
        SetThemed(application, ThemeVariant.Dark, DarkThemeAccentText(accent));
        application.Resources["AccentColor"] = accent;
    }

    /// <summary>Sets the accent text shade for <paramref name="theme"/> (Default: light, and every look without a dark variant).</summary>
    private static void SetThemed(Application application, ThemeVariant theme, Color text)
    {
        var themes = application.Resources.ThemeDictionaries;
        if (!themes.TryGetValue(theme, out var provider) || provider is not ResourceDictionary dictionary)
        {
            dictionary = new ResourceDictionary();
            themes[theme] = dictionary;
        }

        dictionary[TextBrushKey] = new SolidColorBrush(text);
        dictionary[TextColorKey] = text;
    }

    /// <summary>The accent darkened until it reads as text on white (4.5:1), as Windows does in light mode.</summary>
    internal static Color LightThemeAccentText(Color accent)
    {
        var text = Color.FromRgb(accent.R, accent.G, accent.B);
        while (ContrastWithWhite(text) < 4.5)
        {
            text = Darken(text, 0.92);
        }

        return text;
    }

    /// <summary>
    /// The accent for text and strokes in dark: its "Light 2" shade (the shade Windows uses in dark mode), mixed further
    /// toward white until it reads as text on the dark surfaces (4.5:1) and as a stroke where they are lightest (3:1).
    /// </summary>
    internal static Color DarkThemeAccentText(Color accent) => DarkThemeAccentText(accent, SystemLight2(accent));

    /// <summary>
    /// As <see cref="DarkThemeAccentText(Color)"/>, from <paramref name="light2"/>, or, without it, an approximation of
    /// Light 2 (the accent 70/255 lighter in HSL), as off Windows.
    /// </summary>
    internal static Color DarkThemeAccentText(Color accent, Color? light2)
    {
        Color text;
        if (light2 is { } shade)
        {
            text = Color.FromRgb(shade.R, shade.G, shade.B);
        }
        else
        {
            var hsl = Color.FromRgb(accent.R, accent.G, accent.B).ToHsl();
            var approximation = new HslColor(1, hsl.H, hsl.S, Math.Min(1, hsl.L + (70 / 255d))).ToRgb();
            text = Color.FromRgb(approximation.R, approximation.G, approximation.B);
        }

        while (ContrastRatio(text, DarkSurface) < 4.5 || ContrastRatio(text, DarkSurfaceOverLight) < 3)
        {
            text = Mix(text, Colors.White, 0.08);
        }

        return text;
    }

    /// <summary>
    /// The Light 2 shade Windows itself computed for <paramref name="accent"/> (from Explorer's AccentPalette), or null
    /// off Windows, when the palette cannot be read or when it is for another accent (the colour passed in is not the
    /// system's).
    /// </summary>
    private static Color? SystemLight2(Color accent)
    {
        if (!OperatingSystem.IsWindows() || WindowsAccentPalette.TryRead() is not { } palette)
        {
            return null;
        }

        var system = palette[WindowsAccentPalette.Accent];
        if (system.R != accent.R || system.G != accent.G || system.B != accent.B)
        {
            return null;
        }

        var light2 = palette[WindowsAccentPalette.Light2];
        return Color.FromRgb(light2.R, light2.G, light2.B);
    }

    /// <summary>The WCAG contrast ratio of <paramref name="color"/> against white.</summary>
    internal static double ContrastWithWhite(Color color) => ContrastRatio(color, Colors.White);

    /// <summary>The WCAG contrast ratio of two opaque colours.</summary>
    internal static double ContrastRatio(Color first, Color second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(Color color)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
    }

    internal static Color Darken(Color color, double factor) => Color.FromRgb(
        (byte)Math.Round(color.R * factor),
        (byte)Math.Round(color.G * factor),
        (byte)Math.Round(color.B * factor));

    private static Color Mix(Color color, Color with, double amount) => Color.FromRgb(
        (byte)Math.Round(color.R + ((with.R - color.R) * amount)),
        (byte)Math.Round(color.G + ((with.G - color.G) * amount)),
        (byte)Math.Round(color.B + ((with.B - color.B) * amount)));
}
