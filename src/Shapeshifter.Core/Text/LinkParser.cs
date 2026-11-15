namespace Shapeshifter.Core.Text;

/// <summary>Finds web links in copied text.</summary>
/// <remarks>
/// Unlike the original Windows implementation this does not resolve domains through DNS on every copy:
/// a link must either carry an http(s) scheme or start with <c>www.</c>.
/// </remarks>
public static class LinkParser
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"];
    private static readonly char[] TrailingPunctuation = ['.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'', '>'];
    private static readonly char[] LeadingPunctuation = ['(', '[', '{', '"', '\'', '<'];

    public const int MaxLinkLength = 2048;

    public static IReadOnlyList<Uri> ExtractLinks(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var links = new List<Uri>();
        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseLink(token, out var uri) && !links.Contains(uri))
            {
                links.Add(uri);
            }
        }

        return links;
    }

    public static bool TryParseLink(string candidate, out Uri uri)
    {
        uri = null!;
        var word = candidate.Trim().TrimStart(LeadingPunctuation).TrimEnd(TrailingPunctuation);
        if (word.Length is 0 or > MaxLinkLength)
        {
            return false;
        }

        if (word.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            word = "https://" + word;
        }

        if (!Uri.TryCreate(word, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(parsed.Host)
            || (!parsed.Host.Contains('.', StringComparison.Ordinal) && parsed.Host != "localhost")
            || parsed.Host.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    public static bool IsImageLink(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var path = uri.AbsolutePath;
        return ImageExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}
