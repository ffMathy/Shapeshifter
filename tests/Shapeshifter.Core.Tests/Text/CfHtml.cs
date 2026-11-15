using System.Globalization;
using System.Text;

namespace Shapeshifter.Core.Tests.Text;

/// <summary>Builds the Windows "HTML Format" (CF_HTML) clipboard payload the way browsers and Office do.</summary>
internal static class CfHtml
{
    private static string Header(int startHtml, int endHtml, int startFragment, int endFragment) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"Version:0.9\r\nStartHTML:{startHtml:D10}\r\nEndHTML:{endHtml:D10}\r\nStartFragment:{startFragment:D10}\r\nEndFragment:{endFragment:D10}\r\nSourceURL:https://example.com/page?a=1&b=2\r\n");

    public const string StartFragmentMarker = "<!--StartFragment-->";
    public const string EndFragmentMarker = "<!--EndFragment-->";

    public static byte[] Build(string fragment, bool trailingNul = true)
    {
        var prefix = "<html>\r\n<body>\r\n" + StartFragmentMarker;
        var suffix = EndFragmentMarker + "\r\n</body>\r\n</html>";

        // The header has a fixed length because every offset is zero-padded to ten digits.
        var headerLength = Encoding.UTF8.GetByteCount(Header(0, 0, 0, 0));
        var startHtml = headerLength;
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(suffix);

        var header = Header(startHtml, endHtml, startFragment, endFragment);
        var bytes = Encoding.UTF8.GetBytes(header + prefix + fragment + suffix);
        return trailingNul ? [.. bytes, 0] : bytes;
    }

    public static string Html(string fragment) =>
        "<html>\r\n<body>\r\n" + StartFragmentMarker + fragment + EndFragmentMarker + "\r\n</body>\r\n</html>";
}
