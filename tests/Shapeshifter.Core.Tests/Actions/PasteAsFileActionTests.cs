using System.Globalization;
using System.Text;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class PasteAsFileActionTests
{
    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "plain", true },
        { "whitespace", true },
        { "text+html", true },
        { "html-only", false },
        { "empty-text+html", false },
        { "rtf-only", false },
        { "image", true },
        { "file", false },
        { "files", false },
        { "file+html", false },
        { "other", false },
        { "empty", false },
        { "link", true },
        { "image-link", true },
        { "text+image", true },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyForItemsWithPlainTextOrAnImage(string kind, bool expected) =>
        Assert.Equal(expected, new PasteAsFileAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public async Task Execute_WritesUtf8TextFileInTempDirectory()
    {
        using var harness = new ActionTestHarness();
        const string text = "Grüße, 世界 👋\r\nline two\n";
        var capturedAt = new DateTimeOffset(2024, 3, 9, 14, 5, 7, TimeSpan.Zero);
        var item = new ClipboardItem(new ClipboardSnapshot { Text = text }, capturedAt: capturedAt);

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var path = Assert.Single(Assert.Single(harness.Backend.Writes).Files);
        Assert.Equal(Path.Combine(harness.Paths.TempDirectory, "Text files", item.Id.ToString("N")), Path.GetDirectoryName(path));
        Assert.Equal($"Text {capturedAt.LocalDateTime.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture)}.txt", Path.GetFileName(path));
        Assert.EndsWith(".txt", path, StringComparison.Ordinal);

        var bytes = await File.ReadAllBytesAsync(path, ActionTestHarness.Ct);
        var preamble = Encoding.UTF8.GetPreamble();
        var body = bytes.AsSpan().StartsWith(preamble) ? bytes[preamble.Length..] : bytes;
        Assert.Equal(Encoding.UTF8.GetBytes(text), body);
    }

    [Fact]
    public async Task Execute_PutsOnlyTheFileOnTheClipboard_AndPastesIt()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "some text" });

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.Null(written.Text);
        Assert.Null(written.Image);
        Assert.Empty(written.RawFormats);
        Assert.True(File.Exists(Assert.Single(written.Files)));
        Assert.Equal(1, harness.Paste.Calls);
    }

    [Fact]
    public async Task Execute_LeavesTheHistoryAsItWas()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "some text" });

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(item.Id, Assert.Single(harness.History.Items).Id);
    }

    [Fact]
    public async Task Execute_PastedFileIsNotCapturedAgainByTheMonitor()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "some text" });

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);
        var captured = await harness.Monitor.CaptureNowAsync(ActionTestHarness.Ct);

        Assert.Null(captured);
        Assert.Equal(1, harness.History.Count);
    }

    [Fact]
    public async Task Execute_TwiceForTheSameItem_PastesTwice()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "some text" });
        var action = new PasteAsFileAction();

        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);
        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(1, harness.History.Count);
        Assert.Equal(2, harness.Backend.Writes.Count);
        Assert.Equal(2, harness.Paste.Calls);
    }

    /// <summary>
    /// Two different texts copied within the same second must not end up sharing one file: otherwise the older
    /// history item silently starts pointing at the newer text.
    /// </summary>
    [Fact]
    public async Task Execute_ItemsCapturedInTheSameSecond_DoNotOverwriteEachOther()
    {
        using var harness = new ActionTestHarness();
        var capturedAt = new DateTimeOffset(2024, 3, 9, 14, 5, 7, TimeSpan.Zero);
        var first = new ClipboardItem(new ClipboardSnapshot { Text = "first" }, capturedAt: capturedAt);
        var second = new ClipboardItem(new ClipboardSnapshot { Text = "second" }, capturedAt: capturedAt.AddMilliseconds(300));
        var action = new PasteAsFileAction();

        await action.ExecuteAsync(first, harness.Context, ActionTestHarness.Ct);
        await action.ExecuteAsync(second, harness.Context, ActionTestHarness.Ct);

        var firstPath = harness.Backend.Writes[0].Files[0];
        var secondPath = harness.Backend.Writes[1].Files[0];
        Assert.NotEqual(firstPath, secondPath);
        Assert.Equal("first", await File.ReadAllTextAsync(firstPath, ActionTestHarness.Ct));
        Assert.Equal("second", await File.ReadAllTextAsync(secondPath, ActionTestHarness.Ct));
    }

    [Fact]
    public void CanExecute_IsFalseForAnEmptyImage() =>
        Assert.False(new PasteAsFileAction().CanExecute(new ClipboardItem(new ClipboardSnapshot { Image = [] })));

    /// <summary>Firefox copies an image together with a temporary file of it; the image is what gets saved.</summary>
    [Fact]
    public void CanExecute_ForAnImageWithItsTemporaryFile() =>
        Assert.True(new PasteAsFileAction().CanExecute(new ClipboardItem(new ClipboardSnapshot
        {
            Image = Png,
            Files = [Path.Combine(Path.GetTempPath(), "mozilla_x", "picture.png")],
        })));

    [Fact]
    public async Task Execute_ForAnImage_WritesTheImageBytesToAFileInTempDirectory()
    {
        using var harness = new ActionTestHarness();
        var capturedAt = new DateTimeOffset(2024, 3, 9, 14, 5, 7, TimeSpan.Zero);
        var item = new ClipboardItem(new ClipboardSnapshot { Image = Png }, capturedAt: capturedAt);

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        var path = Assert.Single(written.Files);
        Assert.Null(written.Image);
        Assert.Null(written.Text);
        Assert.Equal(Path.Combine(harness.Paths.TempDirectory, "Image files", item.Id.ToString("N")), Path.GetDirectoryName(path));
        Assert.Equal($"Image {capturedAt.LocalDateTime.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture)}.png", Path.GetFileName(path));
        Assert.Equal(Png, await File.ReadAllBytesAsync(path, ActionTestHarness.Ct));
        Assert.Equal(1, harness.Paste.Calls);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 }, ".jpg")]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 1 }, ".gif")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png")]
    [InlineData(new byte[] { 1, 2, 3 }, ".png")]
    public async Task Execute_ForAnImage_NamesTheFileAfterItsFormat(byte[] image, string extension)
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = image });

        await new PasteAsFileAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal(extension, Path.GetExtension(Assert.Single(Assert.Single(harness.Backend.Writes).Files)));
    }

    [Fact]
    public void Description_NamesTheKindOfFile()
    {
        var action = new PasteAsFileAction();

        Assert.Equal("Saves this text to a .txt file and pastes the file.", action.GetDescription(Items.ByName("plain")));
        Assert.Equal("Saves this image to a .png file and pastes the file.", action.GetDescription(Items.ByName("image")));
    }

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
}
