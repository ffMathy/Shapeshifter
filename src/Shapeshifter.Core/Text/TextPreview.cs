using System.Text;

namespace Shapeshifter.Core.Text;

/// <summary>Builds the single-paragraph preview shown for a text item.</summary>
public static class TextPreview
{
    public const int MaxLength = 512;

    public static string Create(string? text, int maxLength = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(text.Length, maxLength));
        var previousWasSpace = false;
        foreach (var character in text.AsSpan().Trim())
        {
            if (builder.Length >= maxLength)
            {
                break;
            }

            var isSpace = char.IsWhiteSpace(character) || char.IsControl(character);
            if (isSpace)
            {
                if (!previousWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }
            }
            else
            {
                builder.Append(character);
            }

            previousWasSpace = isSpace;
        }

        // Never end on half of a surrogate pair (an emoji cut at the limit).
        if (builder.Length > 0 && char.IsHighSurrogate(builder[^1]))
        {
            builder.Length--;
        }

        return builder.ToString().TrimEnd();
    }
}
