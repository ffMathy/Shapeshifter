using System.Text;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class ClipboardItemTests
{
    [Fact]
    public void Constructor_NullSnapshot_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ClipboardItem(null!));

    [Fact]
    public void Constructor_UsesGivenIdAndTimestamp()
    {
        var id = Guid.NewGuid();
        var at = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
        var snapshot = new ClipboardSnapshot { Text = "x" };

        var item = new ClipboardItem(snapshot, id, at);

        Assert.Equal(id, item.Id);
        Assert.Equal(at, item.CapturedAt);
        Assert.Same(snapshot, item.Snapshot);
        Assert.False(item.IsPinned);
    }

    [Fact]
    public void Constructor_DefaultsToFreshIdAndCurrentUtcTime()
    {
        var before = DateTimeOffset.UtcNow;
        var a = new ClipboardItem(new ClipboardSnapshot { Text = "x" });
        var b = new ClipboardItem(new ClipboardSnapshot { Text = "x" });
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, a.Id);
        Assert.NotEqual(a.Id, b.Id);
        Assert.InRange(a.CapturedAt, before, after);
        Assert.Equal(TimeSpan.Zero, a.CapturedAt.Offset);
    }

    [Fact]
    public void Kind_MirrorsSnapshot()
    {
        Assert.Equal(ClipboardContentKind.Text, new ClipboardItem(new ClipboardSnapshot { Text = "x" }).Kind);
        Assert.Equal(ClipboardContentKind.Image, new ClipboardItem(new ClipboardSnapshot { Image = [1] }).Kind);
    }

    [Fact]
    public void Preview_Text_CollapsesWhitespaceToSingleLine()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "  hello\r\n\tworld  \n" });

        Assert.Equal("hello world", item.PreviewText);
        Assert.Equal("Text: hello world", item.ToString());
    }

    [Fact]
    public void Preview_LongText_IsTruncated()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = new string('a', 10_000) });

        Assert.Equal(512, item.PreviewText.Length);
    }

    [Fact]
    public void Preview_File_IsFileName()
    {
        var path = Path.Combine(Path.GetTempPath(), "folder", "report.pdf");

        var item = new ClipboardItem(new ClipboardSnapshot { Files = [path] });

        Assert.Equal(ClipboardContentKind.File, item.Kind);
        Assert.Equal("report.pdf", item.PreviewText);
    }

    [Fact]
    public void Preview_FolderWithTrailingSeparator_IsFolderName()
    {
        var path = Path.Combine(Path.GetTempPath(), "folder", "sub") + Path.DirectorySeparatorChar;

        var item = new ClipboardItem(new ClipboardSnapshot { Files = [path] });

        Assert.Equal("sub", item.PreviewText);
    }

    [Fact]
    public void Preview_FileCollection_IsCount()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Files = ["/a", "/b", "/c"] });

        Assert.Equal(ClipboardContentKind.FileCollection, item.Kind);
        Assert.Equal("3 files", item.PreviewText);
    }

    [Fact]
    public void Preview_Image()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Image = [1, 2, 3] });

        Assert.Equal("Image", item.PreviewText);
        Assert.Equal("Image: Image", item.ToString());
    }

    [Fact]
    public void Preview_HtmlOnly_UsesPlainTextOfHtml()
    {
        var html = Encoding.UTF8.GetBytes("<html><body><b>Hello</b> &amp; welcome</body></html>");

        var item = new ClipboardItem(new ClipboardSnapshot { RawFormats = [new RawClipboardFormat("text/html", html)] });

        Assert.Equal(ClipboardContentKind.Text, item.Kind);
        Assert.Equal("Hello & welcome", item.PreviewText);
    }

    [Fact]
    public void Preview_WindowsCfHtml_StripsHeader()
    {
        var html = Encoding.UTF8.GetBytes("Version:0.9\r\nStartHTML:00000097\r\nEndHTML:00000150\r\n<html><body>Hi there</body></html>");

        var item = new ClipboardItem(new ClipboardSnapshot { RawFormats = [new RawClipboardFormat("HTML Format", html)] });

        Assert.Equal("Hi there", item.PreviewText);
    }

    [Fact]
    public void Preview_RtfOnly_IsFormattedText()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            RawFormats = [new RawClipboardFormat("text/rtf", Encoding.ASCII.GetBytes(@"{\rtf1 Hello}"))],
        });

        Assert.Equal(ClipboardContentKind.Text, item.Kind);
        Assert.Equal("Formatted text", item.PreviewText);
    }

    [Fact]
    public void Preview_EmptyTextWithHtml_UsesHtml()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            Text = string.Empty,
            RawFormats = [new RawClipboardFormat("text/html", Encoding.UTF8.GetBytes("<p>From html</p>"))],
        });

        Assert.Equal("From html", item.PreviewText);
    }

    [Fact]
    public void Preview_Other_ListsFirstThreeFormatNames()
    {
        var item = new ClipboardItem(new ClipboardSnapshot
        {
            RawFormats =
            [
                new RawClipboardFormat("a/one", [1]),
                new RawClipboardFormat("b/two", [2]),
                new RawClipboardFormat("c/three", [3]),
                new RawClipboardFormat("d/four", [4]),
            ],
        });

        Assert.Equal(ClipboardContentKind.Other, item.Kind);
        Assert.Equal("a/one, b/two, c/three", item.PreviewText);
    }

    [Fact]
    public void Preview_FilesWinOverText()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "some text", Files = ["/tmp/x.txt"] });

        Assert.Equal("x.txt", item.PreviewText);
    }

    [Fact]
    public void Preview_TextWinsOverImage()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "caption", Image = [1] });

        Assert.Equal(ClipboardContentKind.Text, item.Kind);
        Assert.Equal("caption", item.PreviewText);
    }
}
