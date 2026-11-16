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
    }

    internal static Color Darken(Color color, double factor) => Color.FromRgb(
        (byte)Math.Round(color.R * factor),
        (byte)Math.Round(color.G * factor),
        (byte)Math.Round(color.B * factor));
}
