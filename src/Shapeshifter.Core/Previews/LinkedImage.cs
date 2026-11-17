using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Previews;

/// <summary>
/// An image that a copied text points to: a web link ending in an image extension, or the path of an image file
/// (issue #21). The picker shows the image behind such a text, so the user sees what the link is of.
/// </summary>
/// <remarks>
/// Only a text that is nothing but the link or path counts: a sentence that mentions an image link is still text first,
/// and is previewed as text.
/// </remarks>
public sealed record LinkedImage
{
    private LinkedImage(Uri? link, string? filePath)
    {
        Link = link;
        FilePath = filePath;
    }

    /// <summary>The web address of the image (http or https), or null for a file.</summary>
    public Uri? Link { get; }

    /// <summary>The full path of the image file, or null for a web link.</summary>
    public string? FilePath { get; }

    /// <summary>Identifies the image, for caching it.</summary>
    public string Key => Link?.AbsoluteUri ?? FilePath!;

    /// <summary>
    /// Where the image is, in a few words: the web site of a link ("i.imgur.com"), or the folder of a file.
    /// </summary>
    public string Location => Link is not null
        ? Link.Host
        : Path.GetDirectoryName(FilePath) is { Length: > 0 } folder ? folder : FilePath!;

    public static LinkedImage ForLink(Uri link)
    {
        ArgumentNullException.ThrowIfNull(link);
        return new LinkedImage(link, null);
    }

    public static LinkedImage ForFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new LinkedImage(null, path);
    }

    /// <summary>The image a copied text item consists of, or null when it is anything else.</summary>
    public static LinkedImage? Find(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Text ? Find(item.Snapshot.Text) : null;
    }

    /// <summary>
    /// The image a text consists of: a single web link to an image ("https://example.com/cat.png"), a file:// address,
    /// or the full path of an image file, optionally in quotes (as Windows Explorer's "Copy as path" gives it).
    /// </summary>
    public static LinkedImage? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var candidate = text.Trim();
        if (candidate.Length > LinkParser.MaxLinkLength || candidate.Contains('\n', StringComparison.Ordinal) || candidate.Contains('\r', StringComparison.Ordinal))
        {
            return null;
        }

        // A path may contain spaces, a link may not.
        if (!candidate.Any(char.IsWhiteSpace) && LinkParser.TryParseLink(candidate, out var link))
        {
            return LinkParser.IsImageLink(link) ? ForLink(link) : null;
        }

        if (candidate.Length >= 2 && candidate[0] == '"' && candidate[^1] == '"')
        {
            candidate = candidate[1..^1].Trim();
        }

        if (candidate.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.IsFile && LinkParser.IsImagePath(uri.LocalPath)
                ? ForFile(uri.LocalPath)
                : null;
        }

        return IsFullPath(candidate) && LinkParser.IsImagePath(candidate) ? ForFile(candidate) : null;
    }

    /// <summary>
    /// A full path on any platform: "C:\…" or "\\server\…" on Windows, "/…" elsewhere. Checked the same way on every
    /// platform, so the result does not depend on where it runs; only an existing file is ever shown.
    /// </summary>
    private static bool IsFullPath(string path) =>
        path.StartsWith('/')
        || path.StartsWith(@"\\", StringComparison.Ordinal)
        || (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '\\' or '/');
}
