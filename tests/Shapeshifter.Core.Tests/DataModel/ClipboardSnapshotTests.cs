using System.Text;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.DataModel;

public sealed class ClipboardSnapshotTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static RawClipboardFormat Raw(string name, string content) => new(name, Encoding.UTF8.GetBytes(content));

    // ---------- Empty / IsEmpty ----------

    [Fact]
    public void Empty_IsEmptyWithEmptyKindAndNoContent()
    {
        var empty = ClipboardSnapshot.Empty;

        Assert.True(empty.IsEmpty);
        Assert.Equal(ClipboardContentKind.Empty, empty.Kind);
        Assert.Null(empty.Text);
        Assert.Null(empty.Image);
        Assert.Empty(empty.Files);
        Assert.Empty(empty.RawFormats);
        Assert.Equal(0, empty.ApproximateSize);
    }

    [Fact]
    public void Empty_IsSingleton()
    {
        Assert.Same(ClipboardSnapshot.Empty, ClipboardSnapshot.Empty);
    }

    [Fact]
    public void NewSnapshot_WithoutContent_IsEmpty()
    {
        Assert.True(new ClipboardSnapshot().IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseForEmptyStringText()
    {
        // An empty string is still a (degenerate) value, unlike null.
        Assert.False(new ClipboardSnapshot { Text = string.Empty }.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseWhenAnyPartIsPresent()
    {
        Assert.False(new ClipboardSnapshot { Text = "a" }.IsEmpty);
        Assert.False(new ClipboardSnapshot { Files = ["/tmp/a"] }.IsEmpty);
        Assert.False(new ClipboardSnapshot { Image = [] }.IsEmpty);
        Assert.False(new ClipboardSnapshot { RawFormats = [Raw("x-custom", "1")] }.IsEmpty);
    }

    // ---------- Kind priority ----------

    [Fact]
    public void Kind_SingleFile_IsFile()
    {
        Assert.Equal(ClipboardContentKind.File, new ClipboardSnapshot { Files = ["/tmp/a.txt"] }.Kind);
    }

    [Fact]
    public void Kind_MultipleFiles_IsFileCollection()
    {
        Assert.Equal(
            ClipboardContentKind.FileCollection,
            new ClipboardSnapshot { Files = ["/tmp/a.txt", "/tmp/b.txt"] }.Kind);
    }

    [Fact]
    public void Kind_FilesWinOverTextImageAndRawFormats()
    {
        var snapshot = new ClipboardSnapshot
        {
            Files = ["/tmp/a.txt"],
            Text = "/tmp/a.txt",
            Image = PngBytes,
            RawFormats = [Raw("text/html", "<b>x</b>")],
        };

        Assert.Equal(ClipboardContentKind.File, snapshot.Kind);
    }

    [Fact]
    public void Kind_ImageWithATemporaryCopyOfItself_IsImage()
    {
        // Firefox's "Copy Image" on Windows: the bitmap plus a temporary file that Firefox deletes later (issue #606).
        var snapshot = new ClipboardSnapshot
        {
            Files = [Path.Combine(Path.GetTempPath(), "Firefox", "picture.png")],
            Image = PngBytes,
        };

        Assert.Equal(ClipboardContentKind.Image, snapshot.Kind);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("picture.png")]
    public void Kind_ImageWithAFileOutsideTheTemporaryDirectoryOrNotAnImage_IsFile(string name)
    {
        // A copied file (for example with its icon as a picture) stays a file, unless it is a temporary image.
        var folder = name.EndsWith(".png", StringComparison.Ordinal)
            ? Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "shapeshifter-not-temp")
            : Path.GetTempPath();
        var snapshot = new ClipboardSnapshot { Files = [Path.Combine(folder, name)], Image = PngBytes };

        Assert.Equal(ClipboardContentKind.File, snapshot.Kind);
    }

    [Fact]
    public void Kind_TextWinsOverImage()
    {
        // Office suites put a rendered bitmap of copied text on the clipboard; it should still read as text.
        var snapshot = new ClipboardSnapshot { Text = "hello", Image = PngBytes };

        Assert.Equal(ClipboardContentKind.Text, snapshot.Kind);
    }

    [Fact]
    public void Kind_WhitespaceOnlyText_IsText()
    {
        Assert.Equal(ClipboardContentKind.Text, new ClipboardSnapshot { Text = "  \n" }.Kind);
    }

    [Fact]
    public void Kind_ImageOnly_IsImage()
    {
        Assert.Equal(ClipboardContentKind.Image, new ClipboardSnapshot { Image = PngBytes }.Kind);
    }

    [Fact]
    public void Kind_EmptyTextWithImage_IsImage()
    {
        Assert.Equal(ClipboardContentKind.Image, new ClipboardSnapshot { Text = string.Empty, Image = PngBytes }.Kind);
    }

    [Fact]
    public void Kind_ImageWinsOverRichRawFormats()
    {
        var snapshot = new ClipboardSnapshot { Image = PngBytes, RawFormats = [Raw("text/html", "<img>")] };

        Assert.Equal(ClipboardContentKind.Image, snapshot.Kind);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("HTML Format")]
    [InlineData("public.html")]
    [InlineData("Rich Text Format")]
    [InlineData("public.rtf")]
    [InlineData("text/rtf")]
    public void Kind_RichOnlyRawFormats_IsText(string richFormat)
    {
        var snapshot = new ClipboardSnapshot { RawFormats = [Raw("x-app/private", "1"), Raw(richFormat, "<b>rich</b>")] };

        Assert.Equal(ClipboardContentKind.Text, snapshot.Kind);
    }

    [Fact]
    public void Kind_OtherRawFormatsOnly_IsOther()
    {
        var snapshot = new ClipboardSnapshot { RawFormats = [Raw("application/x-color", "abc"), Raw("x-special/foo", "1")] };

        Assert.Equal(ClipboardContentKind.Other, snapshot.Kind);
    }

    [Fact]
    public void Kind_EmptyTextWithoutRawFormats_IsOther()
    {
        Assert.Equal(ClipboardContentKind.Other, new ClipboardSnapshot { Text = string.Empty }.Kind);
    }

    [Fact]
    public void Kind_EmptyTextWithRichRawFormat_IsText()
    {
        var snapshot = new ClipboardSnapshot { Text = string.Empty, RawFormats = [Raw("text/html", "<p>x</p>")] };

        Assert.Equal(ClipboardContentKind.Text, snapshot.Kind);
    }

    // ---------- Fingerprint ----------

    [Fact]
    public void Fingerprint_IsUppercaseHexSha256()
    {
        var fingerprint = new ClipboardSnapshot { Text = "hello" }.Fingerprint;

        Assert.Equal(64, fingerprint.Length);
        Assert.Matches("^[0-9A-F]{64}$", fingerprint);
    }

    [Fact]
    public void Fingerprint_IsCachedAndStableAcrossCalls()
    {
        var snapshot = new ClipboardSnapshot { Text = "hello" };

        Assert.Same(snapshot.Fingerprint, snapshot.Fingerprint);
    }

    [Fact]
    public void Fingerprint_IsDeterministicAcrossInstances()
    {
        Assert.Equal(new ClipboardSnapshot { Text = "hello" }.Fingerprint, new ClipboardSnapshot { Text = "hello" }.Fingerprint);
    }

    [Fact]
    public void Fingerprint_SameTextWithDifferentRawFormats_IsEqual()
    {
        var plain = new ClipboardSnapshot { Text = "hello" };
        var withSideFormats = new ClipboardSnapshot
        {
            Text = "hello",
            Image = PngBytes,
            RawFormats =
            [
                Raw("text/html", "<b>hello</b>"),
                Raw("chromium/x-source-url", "https://example.com"),
                Raw("x-timestamp", DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ],
        };

        Assert.Equal(plain.Fingerprint, withSideFormats.Fingerprint);
    }

    [Fact]
    public void Fingerprint_DifferentText_IsDifferent()
    {
        Assert.NotEqual(new ClipboardSnapshot { Text = "hello" }.Fingerprint, new ClipboardSnapshot { Text = "hello!" }.Fingerprint);
    }

    [Fact]
    public void Fingerprint_TextIsCaseAndWhitespaceSensitive()
    {
        Assert.NotEqual(new ClipboardSnapshot { Text = "Hello" }.Fingerprint, new ClipboardSnapshot { Text = "hello" }.Fingerprint);
        Assert.NotEqual(new ClipboardSnapshot { Text = "hello" }.Fingerprint, new ClipboardSnapshot { Text = "hello " }.Fingerprint);
    }

    [Fact]
    public void Fingerprint_SameFiles_IsEqualRegardlessOfSideFormats()
    {
        var a = new ClipboardSnapshot { Files = ["/tmp/a", "/tmp/b"] };
        var b = new ClipboardSnapshot { Files = ["/tmp/a", "/tmp/b"], Text = "file:///tmp/a", RawFormats = [Raw("text/uri-list", "x")] };

        Assert.Equal(a.Fingerprint, b.Fingerprint);
    }

    [Fact]
    public void Fingerprint_FileOrderMatters()
    {
        var ab = new ClipboardSnapshot { Files = ["/tmp/a", "/tmp/b"] };
        var ba = new ClipboardSnapshot { Files = ["/tmp/b", "/tmp/a"] };

        Assert.NotEqual(ab.Fingerprint, ba.Fingerprint);
    }

    [Fact]
    public void Fingerprint_FileBoundariesAreUnambiguous()
    {
        var split = new ClipboardSnapshot { Files = ["/tmp/ab", "/tmp/c"] };
        var merged = new ClipboardSnapshot { Files = ["/tmp/a", "b/tmp/c"] };

        Assert.NotEqual(split.Fingerprint, merged.Fingerprint);
    }

    [Fact]
    public void Fingerprint_TextAndFileWithSameValue_AreDifferent()
    {
        var text = new ClipboardSnapshot { Text = "/tmp/a" };
        var file = new ClipboardSnapshot { Files = ["/tmp/a"] };

        Assert.NotEqual(text.Fingerprint, file.Fingerprint);
    }

    [Fact]
    public void Fingerprint_SameImageBytes_IsEqual()
    {
        var a = new ClipboardSnapshot { Image = [.. PngBytes] };
        var b = new ClipboardSnapshot { Image = [.. PngBytes], RawFormats = [Raw("image/bmp", "other encoding")] };

        Assert.Equal(a.Fingerprint, b.Fingerprint);
    }

    [Fact]
    public void Fingerprint_DifferentImageBytes_IsDifferent()
    {
        byte[] changed = [.. PngBytes];
        changed[^1] ^= 0xFF;

        Assert.NotEqual(new ClipboardSnapshot { Image = PngBytes }.Fingerprint, new ClipboardSnapshot { Image = changed }.Fingerprint);
    }

    [Fact]
    public void Fingerprint_ImageAndTextWithSameBytes_AreDifferent()
    {
        var image = new ClipboardSnapshot { Image = Encoding.UTF8.GetBytes("abc") };
        var text = new ClipboardSnapshot { Text = "abc" };

        Assert.NotEqual(image.Fingerprint, text.Fingerprint);
    }

    [Fact]
    public void Fingerprint_OtherKind_DependsOnRawNamesAndData()
    {
        var baseline = new ClipboardSnapshot { RawFormats = [Raw("x-a", "1")] };
        var sameAgain = new ClipboardSnapshot { RawFormats = [Raw("x-a", "1")] };
        var otherData = new ClipboardSnapshot { RawFormats = [Raw("x-a", "2")] };
        var otherName = new ClipboardSnapshot { RawFormats = [Raw("x-b", "1")] };

        Assert.Equal(baseline.Fingerprint, sameAgain.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, otherData.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, otherName.Fingerprint);
    }

    [Fact]
    public void Fingerprint_RichOnlyWithoutText_DependsOnRichContent()
    {
        var a = new ClipboardSnapshot { RawFormats = [Raw("text/html", "<b>one</b>")] };
        var b = new ClipboardSnapshot { RawFormats = [Raw("text/html", "<b>two</b>")] };

        Assert.NotEqual(a.Fingerprint, b.Fingerprint);
    }

    [Fact]
    public void Fingerprint_RichOnlyWithEmptyText_DependsOnRichContent()
    {
        // Text = "" with only rich formats is classified as Text; two different rich documents must not
        // collapse into the same history entry.
        var a = new ClipboardSnapshot { Text = string.Empty, RawFormats = [Raw("text/html", "<b>one</b>")] };
        var b = new ClipboardSnapshot { Text = string.Empty, RawFormats = [Raw("text/html", "<b>two</b>")] };

        Assert.NotEqual(a.Fingerprint, b.Fingerprint);
    }

    [Fact]
    public void Fingerprint_EmptyIsStableAndDiffersFromEmptyText()
    {
        Assert.Equal(new ClipboardSnapshot().Fingerprint, ClipboardSnapshot.Empty.Fingerprint);
        Assert.NotEqual(ClipboardSnapshot.Empty.Fingerprint, new ClipboardSnapshot { Text = "a" }.Fingerprint);
    }

    // ---------- ToPlainText ----------

    [Fact]
    public void ToPlainText_KeepsOnlyText()
    {
        var snapshot = new ClipboardSnapshot
        {
            Text = "hello",
            Image = PngBytes,
            Files = ["/tmp/a"],
            RawFormats = [Raw("text/html", "<b>hello</b>")],
        };

        var plain = snapshot.ToPlainText();

        Assert.NotSame(snapshot, plain);
        Assert.Equal("hello", plain.Text);
        Assert.Null(plain.Image);
        Assert.Empty(plain.Files);
        Assert.Empty(plain.RawFormats);
        Assert.Equal(ClipboardContentKind.Text, plain.Kind);
        Assert.Equal(new ClipboardSnapshot { Text = "hello" }.Fingerprint, plain.Fingerprint);
    }

    [Fact]
    public void ToPlainText_WithoutText_YieldsEmptyString()
    {
        var plain = new ClipboardSnapshot { Image = PngBytes }.ToPlainText();

        Assert.Equal(string.Empty, plain.Text);
        Assert.False(plain.IsEmpty);
        Assert.Null(plain.Image);
    }

    [Fact]
    public void ToPlainText_DoesNotModifyOriginal()
    {
        var snapshot = new ClipboardSnapshot { Text = "x", Image = PngBytes };
        _ = snapshot.ToPlainText();

        Assert.Same(PngBytes, snapshot.Image);
        Assert.Equal("x", snapshot.Text);
    }

    // ---------- ApproximateSize ----------

    [Fact]
    public void ApproximateSize_CountsTextAsUtf16()
    {
        Assert.Equal(10, new ClipboardSnapshot { Text = "hello" }.ApproximateSize);
    }

    [Fact]
    public void ApproximateSize_SumsAllParts()
    {
        var snapshot = new ClipboardSnapshot
        {
            Text = "abc",                                      // 6
            Image = new byte[100],                              // 100
            Files = ["/a", "/bc"],                              // 4 + 6
            RawFormats = [new RawClipboardFormat("x-y", new byte[7]), new RawClipboardFormat("z", [])], // 7 + 6 + 0 + 2
        };

        Assert.Equal(6 + 100 + 4 + 6 + 7 + 6 + 2, snapshot.ApproximateSize);
    }

    [Fact]
    public void ApproximateSize_DoesNotOverflowForLargeImages()
    {
        var snapshot = new ClipboardSnapshot { Image = new byte[3 * 1024 * 1024], Text = new string('a', 1024 * 1024) };

        Assert.Equal((3L * 1024 * 1024) + (2L * 1024 * 1024), snapshot.ApproximateSize);
    }

    // ---------- RawClipboardFormat ----------

    [Fact]
    public void RawClipboardFormat_ExposesNameAndData()
    {
        byte[] data = [1, 2, 3];
        var format = new RawClipboardFormat("text/html", data);

        Assert.Equal("text/html", format.Name);
        Assert.Same(data, format.Data);
    }
}
