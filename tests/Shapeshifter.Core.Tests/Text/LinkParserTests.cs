using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Text;

public sealed class LinkParserTests
{
    // ---------- TryParseLink: accepted ----------

    [Theory]
    [InlineData("http://example.com", "http://example.com/")]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("https://example.com/path/to/page?q=1&r=2#frag", "https://example.com/path/to/page?q=1&r=2#frag")]
    [InlineData("HTTPS://EXAMPLE.COM/Path", "https://example.com/Path")]
    [InlineData("https://sub.domain.example.co.uk:8443/x", "https://sub.domain.example.co.uk:8443/x")]
    [InlineData("http://127.0.0.1:8080/status", "http://127.0.0.1:8080/status")]
    public void TryParseLink_AcceptsHttpAndHttps(string candidate, string expected)
    {
        Assert.True(LinkParser.TryParseLink(candidate, out var uri));
        Assert.Equal(new Uri(expected), uri);
        Assert.Equal(expected, uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("www.example.com", "https://www.example.com/")]
    [InlineData("WWW.Example.com/page", "https://www.example.com/page")]
    [InlineData("www.example.com/a?b=c", "https://www.example.com/a?b=c")]
    public void TryParseLink_WwwPrefix_GetsHttpsScheme(string candidate, string expected)
    {
        Assert.True(LinkParser.TryParseLink(candidate, out var uri));
        Assert.Equal("https", uri.Scheme);
        Assert.Equal(expected, uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5000/api")]
    public void TryParseLink_AcceptsLocalhost(string candidate)
    {
        Assert.True(LinkParser.TryParseLink(candidate, out var uri));
        Assert.Equal("localhost", uri.Host);
    }

    [Theory]
    [InlineData("https://example.com.")]
    [InlineData("https://example.com,")]
    [InlineData("https://example.com;")]
    [InlineData("https://example.com:")]
    [InlineData("https://example.com!")]
    [InlineData("https://example.com?")]
    [InlineData("https://example.com)")]
    [InlineData("https://example.com]")]
    [InlineData("https://example.com}")]
    [InlineData("https://example.com\"")]
    [InlineData("https://example.com'")]
    [InlineData("https://example.com>")]
    [InlineData("https://example.com).")]
    [InlineData("(https://example.com)")]
    [InlineData("[https://example.com]")]
    [InlineData("{https://example.com}")]
    [InlineData("\"https://example.com\"")]
    [InlineData("'https://example.com'")]
    [InlineData("<https://example.com>")]
    [InlineData("  https://example.com  ")]
    public void TryParseLink_StripsSurroundingPunctuation(string candidate)
    {
        Assert.True(LinkParser.TryParseLink(candidate, out var uri));
        Assert.Equal("https://example.com/", uri.AbsoluteUri);
    }

    [Fact]
    public void TryParseLink_KeepsInnerPunctuation()
    {
        Assert.True(LinkParser.TryParseLink("https://example.com/a.b,c;d!e?f=g.", out var uri));
        Assert.Equal("https://example.com/a.b,c;d!e?f=g", uri.AbsoluteUri);
    }

    [Fact]
    public void TryParseLink_KeepsBalancedParenthesesInPath()
    {
        // Wikipedia-style links end with a closing parenthesis that belongs to the URL.
        Assert.True(LinkParser.TryParseLink("https://en.wikipedia.org/wiki/Mercury_(planet)", out var uri));
        Assert.Equal("https://en.wikipedia.org/wiki/Mercury_(planet)", uri.AbsoluteUri);
    }

    [Fact]
    public void TryParseLink_ParenthesisedWikipediaLink_KeepsInnerParentheses()
    {
        Assert.True(LinkParser.TryParseLink("(https://en.wikipedia.org/wiki/Mercury_(planet))", out var uri));
        Assert.Equal("https://en.wikipedia.org/wiki/Mercury_(planet)", uri.AbsoluteUri);
    }

    // ---------- TryParseLink: rejected ----------

    [Theory]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("javascript://example.com/%0Aalert(1)")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html,<b>x</b>")]
    [InlineData("ssh://git@example.com")]
    public void TryParseLink_RejectsNonHttpSchemes(string candidate)
    {
        Assert.False(LinkParser.TryParseLink(candidate, out _));
    }

    [Theory]
    [InlineData("http://intranet")]
    [InlineData("https://server/path")]
    [InlineData("http://example..com")]
    [InlineData("https://a..b")]
    public void TryParseLink_RejectsHostsWithoutDotOrWithDoubleDot(string candidate)
    {
        Assert.False(LinkParser.TryParseLink(candidate, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData("()")]
    [InlineData("example.com")]
    [InlineData("hello")]
    [InlineData("http://")]
    [InlineData("https://")]
    [InlineData("www.")]
    [InlineData("/relative/path")]
    [InlineData("C:\\Windows\\notepad.exe")]
    public void TryParseLink_RejectsNonLinks(string candidate)
    {
        Assert.False(LinkParser.TryParseLink(candidate, out _));
    }

    [Fact]
    public void TryParseLink_RejectsLinksLongerThanMax()
    {
        var prefix = "https://example.com/";
        var link = prefix + new string('a', LinkParser.MaxLinkLength - prefix.Length + 1);

        Assert.Equal(LinkParser.MaxLinkLength + 1, link.Length);
        Assert.False(LinkParser.TryParseLink(link, out _));
    }

    [Fact]
    public void TryParseLink_AcceptsLinksOfExactlyMaxLength()
    {
        var prefix = "https://example.com/";
        var link = prefix + new string('a', LinkParser.MaxLinkLength - prefix.Length);

        Assert.True(LinkParser.TryParseLink(link, out var uri));
        Assert.Equal(link, uri.AbsoluteUri);
    }

    [Fact]
    public void TryParseLink_MaxLengthIsAppliedAfterPunctuationTrimming()
    {
        var prefix = "https://example.com/";
        var link = "(" + prefix + new string('a', LinkParser.MaxLinkLength - prefix.Length) + ").";

        Assert.True(LinkParser.TryParseLink(link, out _));
    }

    // ---------- ExtractLinks ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    public void ExtractLinks_BlankText_ReturnsEmpty(string? text)
    {
        Assert.Empty(LinkParser.ExtractLinks(text));
    }

    [Fact]
    public void ExtractLinks_NoLinks_ReturnsEmpty()
    {
        Assert.Empty(LinkParser.ExtractLinks("just some words, example.com and mailto:a@b.com"));
    }

    [Fact]
    public void ExtractLinks_FindsLinksInProseInOrder()
    {
        const string text = "See https://example.com/docs, or (www.example.org).\nAlso:\thttp://localhost:3000/ and \"https://b.example.net/x?y=1\".";

        var links = LinkParser.ExtractLinks(text);

        Assert.Equal(
            ["https://example.com/docs", "https://www.example.org/", "http://localhost:3000/", "https://b.example.net/x?y=1"],
            links.Select(l => l.AbsoluteUri));
    }

    [Fact]
    public void ExtractLinks_DeduplicatesEquivalentLinks()
    {
        const string text = "https://example.com https://EXAMPLE.com/ https://example.com. www.example.com https://www.example.com";

        var links = LinkParser.ExtractLinks(text);

        Assert.Equal(["https://example.com/", "https://www.example.com/"], links.Select(l => l.AbsoluteUri));
    }

    [Fact]
    public void ExtractLinks_HttpAndHttpsAreDistinct()
    {
        var links = LinkParser.ExtractLinks("http://example.com https://example.com");

        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void ExtractLinks_SkipsRejectedTokens()
    {
        var links = LinkParser.ExtractLinks("ftp://a.com javascript:alert(1) http://intranet https://ok.example.com");

        Assert.Equal(["https://ok.example.com/"], links.Select(l => l.AbsoluteUri));
    }

    // ---------- IsImageLink ----------

    [Theory]
    [InlineData("https://example.com/cat.png")]
    [InlineData("https://example.com/cat.jpg")]
    [InlineData("https://example.com/cat.jpeg")]
    [InlineData("https://example.com/cat.gif")]
    [InlineData("https://example.com/cat.bmp")]
    [InlineData("https://example.com/cat.webp")]
    [InlineData("https://example.com/a/b/CAT.PNG")]
    [InlineData("https://example.com/cat.png?width=200&format=jpg")]
    [InlineData("https://example.com/cat.jpg#section")]
    public void IsImageLink_TrueForImageExtensions(string link)
    {
        Assert.True(LinkParser.IsImageLink(new Uri(link)));
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/page.html")]
    [InlineData("https://example.com/cat.png.html")]
    [InlineData("https://example.com/image?file=cat.png")]
    [InlineData("https://example.com/png")]
    [InlineData("https://example.com/cat.svg")]
    [InlineData("https://cat.png/")]
    public void IsImageLink_FalseOtherwise(string link)
    {
        Assert.False(LinkParser.IsImageLink(new Uri(link)));
    }

    [Fact]
    public void IsImageLink_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => LinkParser.IsImageLink(null!));
    }
}
