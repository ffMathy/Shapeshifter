using System.Text;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.DataModel;

public sealed class ClipboardFormatClassifierTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47];

    private static RawClipboardFormat Raw(string name, string content = "x") => new(name, Encoding.UTF8.GetBytes(content));

    // ---------- IsPlainText ----------

    [Theory]
    [InlineData("UTF8_STRING")]
    [InlineData("STRING")]
    [InlineData("TEXT")]
    [InlineData("text/plain")]
    [InlineData("text/plain;charset=utf-8")]
    [InlineData("CF_UNICODETEXT")]
    [InlineData("CF_TEXT")]
    [InlineData("UnicodeText")]
    [InlineData("public.utf8-plain-text")]
    [InlineData("public.utf16-plain-text")]
    [InlineData("NSStringPboardType")]
    public void IsPlainText_RecognisesPlatformTextNames(string name)
    {
        Assert.True(ClipboardFormatClassifier.IsPlainText(name));
    }

    [Theory]
    [InlineData("utf8_string")]
    [InlineData("Text/Plain")]
    [InlineData("  CF_UNICODETEXT  ")]
    public void IsPlainText_IsCaseInsensitiveAndIgnoresSurroundingWhitespace(string name)
    {
        Assert.True(ClipboardFormatClassifier.IsPlainText(name));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("HTML Format")]
    [InlineData("image/png")]
    [InlineData("text/uri-list")]
    [InlineData("")]
    public void IsPlainText_FalseForNonPlainText(string name)
    {
        Assert.False(ClipboardFormatClassifier.IsPlainText(name));
    }

    // ---------- IsRichText / IsHtml ----------

    [Theory]
    [InlineData("HTML Format")]
    [InlineData("Rich Text Format")]
    [InlineData("public.html")]
    [InlineData("public.rtf")]
    [InlineData("Apple HTML pasteboard type")]
    [InlineData("text/html")]
    [InlineData("text/rtf")]
    [InlineData("application/rtf")]
    [InlineData("com.apple.flat-rtfd")]
    [InlineData("html format")]
    public void IsRichText_RecognisesHtmlAndRtfNames(string name)
    {
        Assert.True(ClipboardFormatClassifier.IsRichText(name));
    }

    [Theory]
    [InlineData("UTF8_STRING")]
    [InlineData("CF_UNICODETEXT")]
    [InlineData("image/png")]
    [InlineData("chromium/x-web-custom-data")]
    public void IsRichText_FalseForOtherFormats(string name)
    {
        Assert.False(ClipboardFormatClassifier.IsRichText(name));
    }

    [Theory]
    [InlineData("HTML Format", true)]
    [InlineData("text/html", true)]
    [InlineData("public.html", true)]
    [InlineData("Apple HTML pasteboard type", true)]
    [InlineData("text/html;charset=utf-8", true)]
    [InlineData("Rich Text Format", false)]
    [InlineData("public.rtf", false)]
    [InlineData("text/plain", false)]
    public void IsHtml_DistinguishesHtmlFromRtf(string name, bool expected)
    {
        Assert.Equal(expected, ClipboardFormatClassifier.IsHtml(name));
    }

    // ---------- IsFileList ----------

    [Theory]
    [InlineData("text/uri-list")]
    [InlineData("CF_HDROP")]
    [InlineData("FileDrop")]
    [InlineData("FileNameW")]
    [InlineData("public.file-url")]
    [InlineData("NSFilenamesPboardType")]
    public void IsFileList_RecognisesFileListNames(string name)
    {
        Assert.True(ClipboardFormatClassifier.IsFileList(name));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("image/png")]
    [InlineData("x-special/gnome-icon-list")]
    public void IsFileList_FalseForOtherFormats(string name)
    {
        Assert.False(ClipboardFormatClassifier.IsFileList(name));
    }

    // ---------- IsImage ----------

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/bmp")]
    [InlineData("IMAGE/PNG")]
    [InlineData("PNG")]
    [InlineData("CF_DIB")]
    [InlineData("CF_DIBV5")]
    [InlineData("DeviceIndependentBitmap")]
    [InlineData("public.png")]
    [InlineData("public.tiff")]
    [InlineData("NeXT TIFF v4.0 pasteboard type")]
    public void IsImage_RecognisesImageNames(string name)
    {
        Assert.True(ClipboardFormatClassifier.IsImage(name));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("UTF8_STRING")]
    [InlineData("application/x-image")]
    [InlineData("myimage/png")]
    public void IsImage_FalseForOtherFormats(string name)
    {
        Assert.False(ClipboardFormatClassifier.IsImage(name));
    }

    // ---------- ShouldCapture ----------

    [Theory]
    [InlineData("TARGETS")]
    [InlineData("MULTIPLE")]
    [InlineData("TIMESTAMP")]
    [InlineData("SAVE_TARGETS")]
    [InlineData("INCR")]
    [InlineData("targets")]
    public void ShouldCapture_RejectsX11ProtocolTargets(string name)
    {
        Assert.False(ClipboardFormatClassifier.ShouldCapture(name));
    }

    [Theory]
    [InlineData("DataObject")]
    [InlineData("Ole Private Data")]
    [InlineData("Object Descriptor")]
    [InlineData("CF_ENHMETAFILE")]
    [InlineData("CF_METAFILEPICT")]
    [InlineData("CF_BITMAP")]
    [InlineData("CF_PALETTE")]
    [InlineData("EnhancedMetafile")]
    public void ShouldCapture_RejectsWindowsHandleAndOleFormats(string name)
    {
        Assert.False(ClipboardFormatClassifier.ShouldCapture(name));
    }

    [Fact]
    public void ShouldCapture_RejectsMacPromisedFiles()
    {
        Assert.False(ClipboardFormatClassifier.ShouldCapture("com.apple.pasteboard.promised-file-url"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ShouldCapture_RejectsBlankNames(string name)
    {
        Assert.False(ClipboardFormatClassifier.ShouldCapture(name));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("HTML Format")]
    [InlineData("public.html")]
    [InlineData("UTF8_STRING")]
    [InlineData("image/png")]
    [InlineData("chromium/x-web-custom-data")]
    [InlineData("CF_DIB")]
    public void ShouldCapture_AcceptsDataFormats(string name)
    {
        Assert.True(ClipboardFormatClassifier.ShouldCapture(name));
    }

    [Fact]
    public void ShouldCapture_AcceptsSizeUpToLimit()
    {
        Assert.True(ClipboardFormatClassifier.ShouldCapture("text/html", ClipboardFormatClassifier.MaxRawFormatBytes));
    }

    [Fact]
    public void ShouldCapture_RejectsSizeAboveLimit()
    {
        Assert.False(ClipboardFormatClassifier.ShouldCapture("text/html", ClipboardFormatClassifier.MaxRawFormatBytes + 1L));
    }

    [Fact]
    public void MaxRawFormatBytes_Is32MiB()
    {
        Assert.Equal(32 * 1024 * 1024, ClipboardFormatClassifier.MaxRawFormatBytes);
    }

    // ---------- ShouldRestore ----------

    [Theory]
    [InlineData("UTF8_STRING")]
    [InlineData("text/plain")]
    [InlineData("CF_UNICODETEXT")]
    [InlineData("public.utf8-plain-text")]
    public void ShouldRestore_SkipsPlainTextWhenSnapshotHasText(string name)
    {
        var snapshot = new ClipboardSnapshot { Text = "hello" };

        Assert.False(ClipboardFormatClassifier.ShouldRestore(name, snapshot));
    }

    [Fact]
    public void ShouldRestore_SkipsPlainTextEvenWhenTextIsEmptyString()
    {
        Assert.False(ClipboardFormatClassifier.ShouldRestore("UTF8_STRING", new ClipboardSnapshot { Text = string.Empty }));
    }

    [Fact]
    public void ShouldRestore_KeepsPlainTextWhenSnapshotHasNoText()
    {
        var snapshot = new ClipboardSnapshot { RawFormats = [Raw("UTF8_STRING")] };

        Assert.True(ClipboardFormatClassifier.ShouldRestore("UTF8_STRING", snapshot));
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("PNG")]
    [InlineData("CF_DIB")]
    [InlineData("public.png")]
    public void ShouldRestore_SkipsImageFormatsWhenSnapshotHasImage(string name)
    {
        var snapshot = new ClipboardSnapshot { Image = PngBytes };

        Assert.False(ClipboardFormatClassifier.ShouldRestore(name, snapshot));
    }

    [Fact]
    public void ShouldRestore_KeepsImageFormatsWhenSnapshotHasNoImage()
    {
        Assert.True(ClipboardFormatClassifier.ShouldRestore("image/png", new ClipboardSnapshot { Text = "x" }));
    }

    [Theory]
    [InlineData("text/uri-list")]
    [InlineData("CF_HDROP")]
    [InlineData("public.file-url")]
    public void ShouldRestore_SkipsFileListsWhenSnapshotHasFiles(string name)
    {
        var snapshot = new ClipboardSnapshot { Files = ["/tmp/a"] };

        Assert.False(ClipboardFormatClassifier.ShouldRestore(name, snapshot));
    }

    [Fact]
    public void ShouldRestore_KeepsUriListWhenSnapshotHasNoFiles()
    {
        Assert.True(ClipboardFormatClassifier.ShouldRestore("text/uri-list", new ClipboardSnapshot { Text = "x" }));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("HTML Format")]
    [InlineData("Rich Text Format")]
    [InlineData("public.html")]
    [InlineData("chromium/x-web-custom-data")]
    public void ShouldRestore_KeepsRichAndPrivateFormatsAlongsideAllPortableParts(string name)
    {
        var snapshot = new ClipboardSnapshot { Text = "x", Image = PngBytes, Files = ["/tmp/a"] };

        Assert.True(ClipboardFormatClassifier.ShouldRestore(name, snapshot));
    }

    [Theory]
    [InlineData("TARGETS")]
    [InlineData("DataObject")]
    [InlineData("")]
    public void ShouldRestore_NeverRestoresUncapturableFormats(string name)
    {
        Assert.False(ClipboardFormatClassifier.ShouldRestore(name, ClipboardSnapshot.Empty));
    }

    [Fact]
    public void ShouldRestore_ThrowsOnNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => ClipboardFormatClassifier.ShouldRestore("text/html", null!));
    }

    // ---------- FindRichText ----------

    [Fact]
    public void FindRichText_ReturnsFirstRichFormatInOrder()
    {
        var html = Raw("HTML Format", "<b>a</b>");
        var rtf = Raw("Rich Text Format", "{\\rtf1 a}");
        var snapshot = new ClipboardSnapshot { RawFormats = [Raw("x-private"), html, rtf] };

        Assert.Same(html, ClipboardFormatClassifier.FindRichText(snapshot));
    }

    [Fact]
    public void FindRichText_ReturnsNullWithoutRichFormats()
    {
        var snapshot = new ClipboardSnapshot { Text = "x", RawFormats = [Raw("UTF8_STRING"), Raw("image/png")] };

        Assert.Null(ClipboardFormatClassifier.FindRichText(snapshot));
    }

    [Fact]
    public void FindRichText_ThrowsOnNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => ClipboardFormatClassifier.FindRichText(null!));
    }
}
