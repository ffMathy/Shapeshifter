using System.Text;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Text;

public sealed class HtmlTextTests
{
    private static readonly string NL = Environment.NewLine;

    // ---------- Decode ----------

    [Fact]
    public void Decode_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlText.Decode(null!));
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, HtmlText.Decode([]));
    }

    [Fact]
    public void Decode_PlainUtf8Html_IsReturnedAsIs()
    {
        const string html = "<meta charset=\"utf-8\"><b>Grüße</b> &amp; 日本";

        Assert.Equal(html, HtmlText.Decode(Encoding.UTF8.GetBytes(html)));
    }

    [Fact]
    public void Decode_WindowsCfHtml_StripsHeader()
    {
        var data = CfHtml.Build("<b>bold</b> text");

        var html = HtmlText.Decode(data);

        Assert.Equal(CfHtml.Html("<b>bold</b> text"), html);
        Assert.DoesNotContain("Version:", html);
        Assert.DoesNotContain("StartFragment:", html);
        Assert.DoesNotContain("SourceURL", html);
    }

    [Fact]
    public void Decode_WindowsCfHtml_WithNonAsciiFragment()
    {
        Assert.Equal(CfHtml.Html("<i>Ünïcødé ✓</i>"), HtmlText.Decode(CfHtml.Build("<i>Ünïcødé ✓</i>")));
    }

    [Fact]
    public void Decode_WindowsCfHtmlWithoutTrailingNul_StripsHeader()
    {
        Assert.Equal(CfHtml.Html("x"), HtmlText.Decode(CfHtml.Build("x", trailingNul: false)));
    }

    [Fact]
    public void Decode_HeaderWithoutAnyMarkup_ReturnsEmpty()
    {
        var data = Encoding.UTF8.GetBytes("Version:0.9\r\nStartHTML:-1\r\nEndHTML:-1\r\n");

        Assert.Equal(string.Empty, HtmlText.Decode(data));
    }

    [Fact]
    public void Decode_VersionTextNotAtStart_IsNotTreatedAsHeader()
    {
        const string html = "<p>Version:1.0 released</p>";

        Assert.Equal(html, HtmlText.Decode(Encoding.UTF8.GetBytes(html)));
    }

    [Fact]
    public void Decode_Utf16LittleEndianWithBom()
    {
        var data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<b>Firefox ünicode</b>")).ToArray();

        Assert.Equal(0xFF, data[0]);
        Assert.Equal("<b>Firefox ünicode</b>", HtmlText.Decode(data));
    }

    [Fact]
    public void Decode_Utf16BigEndianWithBom()
    {
        var data = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("<i>big</i>")).ToArray();

        Assert.Equal(0xFE, data[0]);
        Assert.Equal("<i>big</i>", HtmlText.Decode(data));
    }

    [Fact]
    public void Decode_Utf16WithTrailingNul_IsTrimmed()
    {
        var data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<p>a</p>\0")).ToArray();

        Assert.Equal("<p>a</p>", HtmlText.Decode(data));
    }

    [Fact]
    public void Decode_TrailingNuls_AreTrimmed()
    {
        Assert.Equal("<p>x</p>", HtmlText.Decode([.. Encoding.UTF8.GetBytes("<p>x</p>"), 0, 0, 0]));
    }

    [Fact]
    public void Decode_OnlyBom_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, HtmlText.Decode([0xFF, 0xFE]));
    }

    // ---------- ToPlainText ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ToPlainText_BlankInput_ReturnsEmpty(string? html)
    {
        Assert.Equal(string.Empty, HtmlText.ToPlainText(html!));
    }

    [Fact]
    public void ToPlainText_StripsTags()
    {
        Assert.Equal("Hello bold world", HtmlText.ToPlainText("<span>Hello <b>bold</b> <a href=\"x\">world</a></span>"));
    }

    [Fact]
    public void ToPlainText_PlainTextInput_IsReturned()
    {
        Assert.Equal("no markup here", HtmlText.ToPlainText("no markup here"));
    }

    [Fact]
    public void ToPlainText_RemovesScriptsAndStyles()
    {
        const string html =
            "<html><head><style>body { color: red; }</style><script>alert('x');</script></head>" +
            "<body>Visible<script type=\"text/javascript\">var hidden = 1;</script></body></html>";

        var text = HtmlText.ToPlainText(html);

        Assert.Equal("Visible", text);
    }

    [Fact]
    public void ToPlainText_DecodesEntities()
    {
        Assert.Equal("Fish & Chips <3 \"quoted\" © é €", HtmlText.ToPlainText("Fish &amp; Chips &lt;3 &quot;quoted&quot; &copy; &#233; &#x20AC;"));
    }

    [Fact]
    public void ToPlainText_DecodesEntitiesOnlyOnce()
    {
        // "&amp;lt;" is the literal text "&lt;", not "<".
        Assert.Equal("&lt;tag&gt;", HtmlText.ToPlainText("<p>&amp;lt;tag&amp;gt;</p>"));
    }

    [Fact]
    public void ToPlainText_Paragraphs_BecomeLines()
    {
        Assert.Equal($"First{NL}Second", HtmlText.ToPlainText("<p>First</p><p>Second</p>"));
    }

    [Fact]
    public void ToPlainText_LineBreaks_BecomeLines()
    {
        Assert.Equal($"one{NL}two{NL}three", HtmlText.ToPlainText("one<br>two<br/>three"));
    }

    [Fact]
    public void ToPlainText_BlockElements_BecomeLines()
    {
        const string html = "<h1>Title</h1><div>Body</div><ul><li>a</li><li>b</li></ul>" +
                            "<table><tr><td>c1</td><td>c2</td></tr><tr><td>d</td></tr></table><h6>End</h6>";

        Assert.Equal($"Title{NL}Body{NL}a{NL}b{NL}c1c2{NL}d{NL}End", HtmlText.ToPlainText(html));
    }

    [Fact]
    public void ToPlainText_InlineElements_DoNotBreakLines()
    {
        Assert.Equal("a b c", HtmlText.ToPlainText("<span>a</span> <i>b</i> <em>c</em>"));
    }

    [Fact]
    public void ToPlainText_DropsEmptyLinesAndTrimsEachLine()
    {
        Assert.Equal($"a{NL}b", HtmlText.ToPlainText("<p>   a   </p><p>  </p><br><br><div>\r\n b \r\n</div>"));
    }

    [Fact]
    public void ToPlainText_NormalisesSourceNewlinesToEnvironmentNewLine()
    {
        Assert.Equal($"x{NL}y", HtmlText.ToPlainText("<pre>x\r\ny</pre>"));
    }

    [Fact]
    public void ToPlainText_OfDecodedWindowsCfHtml_IsFragmentText()
    {
        var html = HtmlText.Decode(CfHtml.Build("<p>Copied from <b>Edge</b></p><p>Line 2</p>"));

        var text = HtmlText.ToPlainText(html);

        Assert.Equal($"Copied from Edge{NL}Line 2", text);
    }

    [Fact]
    public void ToPlainText_MalformedHtml_DoesNotThrow()
    {
        Assert.Equal("unclosed bold", HtmlText.ToPlainText("<div><b>unclosed <i>bold"));
    }
}
