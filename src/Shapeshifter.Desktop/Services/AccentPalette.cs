using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Colours the picker with the operating system's accent colour, as the original Shapeshifter did, and follows it
/// when the user changes it. Where the platform reports no accent colour the palette in Theme.axaml stays.
/// </summary>
public static class AccentPalette
{
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

        // For the looks: a faint tint for selected rows, and the accent darkened until it reads as text on white
        // (4.5:1) and as a fill under white text.
        application.Resources["AccentSoftBrush"] = new SolidColorBrush(Color.FromArgb(0x24, accent.R, accent.G, accent.B));
        application.Resources["AccentFaintBrush"] = new SolidColorBrush(Color.FromArgb(0x14, accent.R, accent.G, accent.B));
        var text = accent;
        while (ContrastWithWhite(text) < 4.5)
        {
            text = Darken(text, 0.92);
        }

        application.Resources["AccentTextBrush"] = new SolidColorBrush(text);
        application.Resources["AccentTextColor"] = text;
        application.Resources["AccentColor"] = accent;
    }

    /// <summary>The WCAG contrast ratio of <paramref name="color"/> against white.</summary>
    internal static double ContrastWithWhite(Color color)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        var luminance = (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
        return 1.05 / (luminance + 0.05);
    }

    internal static Color Darken(Color color, double factor) => Color.FromRgb(
        (byte)Math.Round(color.R * factor),
        (byte)Math.Round(color.G * factor),
        (byte)Math.Round(color.B * factor));
}
