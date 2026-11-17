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
        { "image", false },
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
    public void CanExecute_OnlyForItemsWithPlainText(string kind, bool expected) =>
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
        Assert.Equal($"Text {capturedAt.LocalDateTime:yyyy-MM-dd HH.mm.ss}.txt", Path.GetFileName(path));
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
}
