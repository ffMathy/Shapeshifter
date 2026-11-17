using System.Globalization;

namespace Shapeshifter.Core.Text;

/// <summary>A colour with 8-bit channels.</summary>
public readonly record struct ParsedColor(byte R, byte G, byte B, byte A = 255);

/// <summary>Recognises copied text that is nothing but a colour code, so the picker can show a swatch of it.</summary>
/// <remarks>
/// Accepted: CSS hex colours (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>) and the functional
/// <c>rgb()</c> and <c>rgba()</c> notations with commas or spaces (channels 0–255 or percentages, alpha 0–1 or a
/// percentage). Surrounding whitespace and a trailing semicolon are ignored; anything else in the text means it is
/// not a colour.
/// </remarks>
public static class ColorParser
{
    /// <summary>Longer texts are never just a colour, and are not looked at.</summary>
    private const int MaxLength = 64;

    public static bool TryParse(string? text, out ParsedColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxLength)
        {
            return false;
        }

        var value = text.Trim().TrimEnd(';').TrimEnd();
        if (value.StartsWith('#'))
        {
            return TryParseHex(value.AsSpan(1), out color);
        }

        return TryParseFunctional(value, out color);
    }

    private static bool TryParseHex(ReadOnlySpan<char> hex, out ParsedColor color)
    {
        color = default;
        foreach (var c in hex)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        switch (hex.Length)
        {
            case 3:
            case 4:
                var r = Nibble(hex[0]);
                var g = Nibble(hex[1]);
                var b = Nibble(hex[2]);
                var a = hex.Length == 4 ? Nibble(hex[3]) : 0xF;
                color = new ParsedColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), (byte)(a * 17));
                return true;
            case 6:
            case 8:
                color = new ParsedColor(
                    Byte(hex[..2]),
                    Byte(hex.Slice(2, 2)),
                    Byte(hex.Slice(4, 2)),
                    hex.Length == 8 ? Byte(hex.Slice(6, 2)) : (byte)255);
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseFunctional(string value, out ParsedColor color)
    {
        color = default;
        var open = value.IndexOf('(', StringComparison.Ordinal);
        if (open < 0 || !value.EndsWith(')'))
        {
            return false;
        }

        var name = value[..open].Trim();
        var hasAlphaName = name.Equals("rgba", StringComparison.OrdinalIgnoreCase);
        if (!hasAlphaName && !name.Equals("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // rgb(1, 2, 3), rgb(1 2 3), rgb(1 2 3 / 50%) and rgba(1, 2, 3, 0.5).
        var body = value[(open + 1)..^1];
        string[] parts;
        string? alpha = null;
        if (body.Contains(',', StringComparison.Ordinal))
        {
            parts = body.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 4)
            {
                alpha = parts[3];
                parts = parts[..3];
            }
        }
        else
        {
            var slash = body.Split('/', StringSplitOptions.TrimEntries);
            if (slash.Length > 2)
            {
                return false;
            }

            alpha = slash.Length == 2 ? slash[1] : null;
            parts = slash[0].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (parts.Length != 3)
        {
            return false;
        }

        if (!TryChannel(parts[0], out var r) || !TryChannel(parts[1], out var g) || !TryChannel(parts[2], out var b))
        {
            return false;
        }

        byte a = 255;
        if (alpha is not null && !TryAlpha(alpha, out a))
        {
            return false;
        }

        color = new ParsedColor(r, g, b, a);
        return true;
    }

    private static bool TryChannel(string text, out byte channel)
    {
        channel = 0;
        if (text.EndsWith('%'))
        {
            if (!TryNumber(text[..^1], out var percent) || percent is < 0 or > 100)
            {
                return false;
            }

            channel = (byte)Math.Round(percent * 255 / 100, MidpointRounding.AwayFromZero);
            return true;
        }

        if (!TryNumber(text, out var number) || number is < 0 or > 255)
        {
            return false;
        }

        channel = (byte)Math.Round(number, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryAlpha(string text, out byte alpha)
    {
        alpha = 0;
        double fraction;
        if (text.EndsWith('%'))
        {
            if (!TryNumber(text[..^1], out var percent))
            {
                return false;
            }

            fraction = percent / 100;
        }
        else if (!TryNumber(text, out fraction))
        {
            return false;
        }

        if (fraction is < 0 or > 1)
        {
            return false;
        }

        alpha = (byte)Math.Round(fraction * 255, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryNumber(string text, out double number) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number);

    private static int Nibble(char c) => Convert.ToInt32(c.ToString(), 16);

    private static byte Byte(ReadOnlySpan<char> pair) => byte.Parse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
