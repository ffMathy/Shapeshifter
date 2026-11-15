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

    /// <summary>How much of a text is searched for links.</summary>
    public const int MaxScannedLength = 64 * 1024;

    public static IReadOnlyList<Uri> ExtractLinks(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        // Only the beginning of very large texts is scanned; this runs for every copied item.
        if (text.Length > MaxScannedLength)
        {
            text = text[..MaxScannedLength];
        }

        var links = new List<Uri>();
        var seen = new HashSet<Uri>();
        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseLink(token, out var uri) && seen.Add(uri))
            {
                links.Add(uri);
            }
        }

        return links;
    }

    public static bool TryParseLink(string candidate, out Uri uri)
    {
        uri = null!;
        var word = TrimTrailing(candidate.Trim().TrimStart(LeadingPunctuation));
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

    /// <summary>
    /// Removes trailing punctuation, but keeps a closing bracket that balances an opening one inside the link
    /// (for example <c>https://en.wikipedia.org/wiki/Mercury_(planet)</c>).
    /// </summary>
    private static string TrimTrailing(string word)
    {
        while (word.Length > 0 && Array.IndexOf(TrailingPunctuation, word[^1]) >= 0)
        {
            var last = word[^1];
            var opening = last switch { ')' => '(', ']' => '[', '}' => '{', _ => '\0' };
            if (opening != '\0' && word.Count(c => c == opening) >= word.Count(c => c == last))
            {
                break;
            }

            word = word[..^1];
        }

        return word;
    }

    public static bool IsImageLink(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var path = uri.AbsolutePath;
        return ImageExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}
