using System.Text;
using HtmlAgilityPack;

namespace Shapeshifter.Core.Text;

/// <summary>Extracts readable plain text from clipboard HTML.</summary>
public static class HtmlText
{
    /// <summary>
    /// Decodes clipboard HTML bytes. Windows prefixes "HTML Format" with a CF_HTML header
    /// (<c>Version:0.9 StartHTML:...</c>), which is stripped; UTF-16 data (some Linux browsers) is detected by its BOM.
    /// </summary>
    public static string Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string html;
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
        {
            html = Encoding.Unicode.GetString(data, 2, data.Length - 2);
        }
        else if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
        {
            html = Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
        }
        else
        {
            html = Encoding.UTF8.GetString(data);
        }

        html = html.TrimEnd('\0');
        if (html.StartsWith("Version:", StringComparison.Ordinal))
        {
            var start = html.IndexOf('<', StringComparison.Ordinal);
            html = start >= 0 ? html[start..] : string.Empty;
        }

        return html;
    }

    public static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        foreach (var node in document.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
        {
            node.Remove();
        }

        foreach (var node in document.DocumentNode.SelectNodes("//br|//p|//div|//li|//tr|//h1|//h2|//h3|//h4|//h5|//h6") ?? Enumerable.Empty<HtmlNode>())
        {
            node.AppendChild(document.CreateTextNode("\n"));
        }

        var text = HtmlEntity.DeEntitize(document.DocumentNode.InnerText) ?? string.Empty;
        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);
        return string.Join(Environment.NewLine, lines);
    }
}
