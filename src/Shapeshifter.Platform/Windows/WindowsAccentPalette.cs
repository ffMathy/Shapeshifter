using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Shapeshifter.Platform.Windows;

/// <summary>
/// The shades Windows derives from the accent colour (Settings, Personalisation, Colours), as Explorer stores them: the
/// same shades the Settings app and other native apps use, such as "Light 2" for toggles and accent text in dark mode.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsAccentPalette
{
    /// <summary>The index of "Light 2" in the palette, the accent shade of text and fills in dark mode.</summary>
    public const int Light2 = 1;

    /// <summary>The index of the accent colour itself in the palette.</summary>
    public const int Accent = 3;

    private const int Entries = 8;

    /// <summary>
    /// Reads the palette from <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent</c>, value
    /// <c>AccentPalette</c>: 8 entries of R, G, B and an unused byte, from Light 3, Light 2, Light 1, the accent,
    /// Dark 1, Dark 2 to Dark 3 (the eighth is unused). Returns null when it is missing or malformed.
    /// </summary>
    public static (byte R, byte G, byte B)[]? TryRead()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            return Parse(key?.GetValue("AccentPalette") as byte[]);
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    /// <summary>Splits the registry value into its entries, or returns null when it is not 8 entries of 4 bytes.</summary>
    public static (byte R, byte G, byte B)[]? Parse(byte[]? value)
    {
        if (value is null || value.Length < Entries * 4)
        {
            return null;
        }

        var palette = new (byte R, byte G, byte B)[Entries];
        for (var i = 0; i < Entries; i++)
        {
            palette[i] = (value[i * 4], value[(i * 4) + 1], value[(i * 4) + 2]);
        }

        return palette;
    }
}
