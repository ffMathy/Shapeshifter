using System.Text;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Tests.Text;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.DataModel;

public sealed class ClipboardItemTests
{
    private static RawClipboardFormat Raw(string name, string content = "x") => new(name, Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Constructor_UsesGivenIdAndTimestamp()
    {
        var id = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
        var snapshot = new ClipboardSnapshot { Text = "a" };

        var item = new ClipboardItem(snapshot, id, capturedAt);

        Assert.Equal(id, item.Id);
        Assert.Equal(capturedAt, item.CapturedAt);
        Assert.Same(snapshot, item.Snapshot);
        Assert.False(item.IsPinned);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIdAndCurrentTimestampByDefault()
    {
        var before = DateTimeOffset.UtcNow;
        var first = new ClipboardItem(new ClipboardSnapshot { Text = "a" });
        var second = new ClipboardItem(new ClipboardSnapshot { Text = "a" });
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.InRange(first.CapturedAt, before, after);
        Assert.Equal(TimeSpan.Zero, first.CapturedAt.Offset);
    }

    [Fact]
    public void Constructor_ThrowsOnNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => new ClipboardItem(null!));
    }

    [Fact]
    public void Kind_MirrorsSnapshotKind()
    {
        Assert.Equal(ClipboardContentKind.Image, new ClipboardItem(new ClipboardSnapshot { Image = [1] }).Kind);
    }

    [Fact]
    public void PreviewText_Text_CollapsesWhitespace()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "  first line\r\n\tsecond   line  " });

        Assert.Equal("first line second line", item.PreviewText);
    }

    [Fact]
    public void PreviewText_LongText_IsTruncatedToMaxLength()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = new string('a', 10_000) });

        Assert.Equal(TextPreview.MaxLength, item.PreviewText.Length);
    }

    [Fact]
    public void PreviewText_TextWithImage_UsesText()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = "caption", Image = [1, 2] });

        Assert.Equal("caption", item.PreviewText);
    }

    [Fact]
    public void PreviewText_SingleFile_IsFileName()
    {
        var path = Path.Combine(Path.GetTempPath(), "folder", "report.pdf");

        Assert.Equal("report.pdf", new ClipboardItem(new ClipboardSnapshot { Files = [path] }).PreviewText);
    }

    [Fact]
    public void PreviewText_SingleFolderWithTrailingSeparator_IsFolderName()
    {
        var path = Path.Combine(Path.GetTempPath(), "photos") + Path.DirectorySeparatorChar;

        Assert.Equal("photos", new ClipboardItem(new ClipboardSnapshot { Files = [path] }).PreviewText);
    }

    [Fact]
    public void PreviewText_SingleFolderWithAltTrailingSeparator_IsFolderName()
    {
        var path = Path.Combine(Path.GetTempPath(), "photos") + Path.AltDirectorySeparatorChar;

        Assert.Equal("photos", new ClipboardItem(new ClipboardSnapshot { Files = [path] }).PreviewText);
    }

    [Fact]
    public void PreviewText_MultipleFiles_IsCount()
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Files = ["/a", "/b", "/c"] });

        Assert.Equal("3 files", item.PreviewText);
    }

    [Fact]
    public void PreviewText_Image_IsImage()
    {
        Assert.Equal("Image", new ClipboardItem(new ClipboardSnapshot { Image = [1, 2, 3] }).PreviewText);
    }

    [Fact]
    public void PreviewText_HtmlOnly_IsPlainTextOfHtml()
    {
        var snapshot = new ClipboardSnapshot
        {
            RawFormats = [Raw("text/html", "<html><body><p>Hello &amp; <b>welcome</b></p><p>Second</p></body></html>")],
        };

        Assert.Equal("Hello & welcome Second", new ClipboardItem(snapshot).PreviewText);
    }

    [Fact]
    public void PreviewText_HtmlOnlyWithEmptyText_IsPlainTextOfHtml()
    {
        var snapshot = new ClipboardSnapshot { Text = string.Empty, RawFormats = [Raw("public.html", "<div>Mac rich</div>")] };

        Assert.Equal("Mac rich", new ClipboardItem(snapshot).PreviewText);
    }

    [Fact]
    public void PreviewText_WindowsCfHtmlOnly_StripsHeader()
    {
        var snapshot = new ClipboardSnapshot { RawFormats = [new RawClipboardFormat("HTML Format", CfHtml.Build("<b>Windows</b> text"))] };

        Assert.Equal("Windows text", new ClipboardItem(snapshot).PreviewText);
    }

    [Fact]
    public void PreviewText_RtfOnly_IsFormattedText()
    {
        var snapshot = new ClipboardSnapshot { RawFormats = [Raw("Rich Text Format", "{\\rtf1\\ansi hello}")] };

        Assert.Equal("Formatted text", new ClipboardItem(snapshot).PreviewText);
    }

    [Fact]
    public void PreviewText_Other_ListsFirstThreeFormatNames()
    {
        var snapshot = new ClipboardSnapshot
        {
            RawFormats = [Raw("x-a"), Raw("x-b"), Raw("x-c"), Raw("x-d")],
        };

        Assert.Equal("x-a, x-b, x-c", new ClipboardItem(snapshot).PreviewText);
    }

    [Fact]
    public void PreviewText_Empty_IsEmptyString()
    {
        Assert.Equal(string.Empty, new ClipboardItem(ClipboardSnapshot.Empty).PreviewText);
    }

    [Fact]
    public void ToString_CombinesKindAndPreview()
    {
        Assert.Equal("Text: hi there", new ClipboardItem(new ClipboardSnapshot { Text = "hi\nthere" }).ToString());
    }
}
