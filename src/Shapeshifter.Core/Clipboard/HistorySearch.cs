using System.Globalization;
using System.Text;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Clipboard;

/// <summary>Matches history items against what the user typed in the picker's search box (issue #458).</summary>
/// <remarks>
/// <para>
/// A query matches an item when every word of it occurs somewhere in the item, in any order. Matching ignores case
/// and accents ("cafe" finds "Café").
/// </para>
/// <para>
/// An item is searched by its whole text (not only the preview), the plain text of copied HTML, the paths of copied
/// files, its kind ("image", "file") and the application and window it was copied from.
/// </para>
/// </remarks>
public static class HistorySearch
{
    /// <summary>Only this much of a very long text is searched, so searching stays instant.</summary>
    public const int MaxContentLength = 200_000;

    /// <summary>
    /// The normalized text of the item's content. It never changes for an item, so callers cache it. The place it was
    /// copied from can change (copying it again updates it), so <see cref="CreateSearchText"/> adds that each time.
    /// </summary>
    public static string CreateContentText(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var snapshot = item.Snapshot;
        var builder = new StringBuilder();
        Append(builder, item.PreviewText);
        Append(builder, KindWord(item.Kind));

        if (!string.IsNullOrEmpty(snapshot.Text))
        {
            Append(builder, snapshot.Text.Length > MaxContentLength ? snapshot.Text[..MaxContentLength] : snapshot.Text);
        }
        else if (ClipboardFormatClassifier.FindRichText(snapshot) is { } rich && ClipboardFormatClassifier.IsHtml(rich.Name))
        {
            var plain = HtmlText.ToPlainText(HtmlText.Decode(rich.Data));
            Append(builder, plain.Length > MaxContentLength ? plain[..MaxContentLength] : plain);
        }

        foreach (var file in snapshot.Files)
        {
            Append(builder, file);
        }

        return Normalize(builder.ToString());
    }

    /// <summary>The text an item is searched by: <paramref name="contentText"/> and where the item was copied from.</summary>
    public static string CreateSearchText(string contentText, ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(contentText);
        ArgumentNullException.ThrowIfNull(item);
        if (item.Source is not { } source)
        {
            return contentText;
        }

        return contentText + "\n" + Normalize($"{source.ApplicationName}\n{source.WindowTitle}");
    }

    /// <summary>Splits a query into normalized words; an empty array matches every item.</summary>
    public static string[] ParseQuery(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : Normalize(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>Whether every word of <paramref name="terms"/> (from <see cref="ParseQuery"/>) occurs in <paramref name="searchText"/>.</summary>
    public static bool Matches(string searchText, IReadOnlyList<string> terms)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(terms);
        foreach (var term in terms)
        {
            if (!searchText.Contains(term, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Lower case without accents, so matching is a plain ordinal search.</summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string KindWord(ClipboardContentKind kind) => kind switch
    {
        ClipboardContentKind.Image => "image",
        ClipboardContentKind.File => "file",
        ClipboardContentKind.FileCollection => "files",
        _ => string.Empty,
    };

    private static void Append(StringBuilder builder, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(value);
    }
}
