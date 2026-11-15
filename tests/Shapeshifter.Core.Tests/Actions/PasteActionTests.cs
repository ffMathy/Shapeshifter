using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class PasteActionTests
{
    [Theory]
    [MemberData(nameof(Items.AllKinds), MemberType = typeof(Items))]
    public void CanExecute_IsTrueForEveryKind(string kind) =>
        Assert.True(new PasteAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public async Task Execute_WritesTheFullSnapshot_ThenSimulatesPaste()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(Items.Snapshot("text+html"));

        await new PasteAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.Same(item.Snapshot, written);
        Assert.Equal(1, harness.Paste.Calls);
        Assert.Equal(["write", "paste"], harness.EventLog);
    }

    [Fact]
    public async Task Execute_MovesTheItemToTheTopOfTheUnpinnedSection()
    {
        using var harness = new ActionTestHarness();
        var pinned = harness.AddToHistory(new ClipboardSnapshot { Text = "pinned" });
        harness.History.SetPinned(pinned.Id, pinned: true);
        var old = harness.AddToHistory(new ClipboardSnapshot { Text = "old" });
        var newer = harness.AddToHistory(new ClipboardSnapshot { Text = "newer" });

        await new PasteAction().ExecuteAsync(old, harness.Context, ActionTestHarness.Ct);

        Assert.Equal([pinned.Id, old.Id, newer.Id], harness.History.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Execute_PinnedItem_StaysWhereItIs()
    {
        using var harness = new ActionTestHarness();
        var first = harness.AddToHistory(new ClipboardSnapshot { Text = "first" });
        var second = harness.AddToHistory(new ClipboardSnapshot { Text = "second" });
        harness.History.SetPinned(first.Id, pinned: true);
        harness.History.SetPinned(second.Id, pinned: true);
        var unpinned = harness.AddToHistory(new ClipboardSnapshot { Text = "unpinned" });

        await new PasteAction().ExecuteAsync(second, harness.Context, ActionTestHarness.Ct);

        Assert.Equal([first.Id, second.Id, unpinned.Id], harness.History.Items.Select(item => item.Id));
        Assert.Same(second.Snapshot, Assert.Single(harness.Backend.Writes));
        Assert.Equal(1, harness.Paste.Calls);
    }

    [Fact]
    public async Task Execute_ItemNotInHistory_StillPastes_AndLeavesHistoryUnchanged()
    {
        using var harness = new ActionTestHarness();
        var existing = harness.AddToHistory(new ClipboardSnapshot { Text = "existing" });
        var detached = Items.ByName("image");

        await new PasteAction().ExecuteAsync(detached, harness.Context, ActionTestHarness.Ct);

        Assert.Same(detached.Snapshot, Assert.Single(harness.Backend.Writes));
        Assert.Equal(1, harness.Paste.Calls);
        Assert.Equal([existing.Id], harness.History.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Execute_SelfWrittenContentIsNotCapturedAgain()
    {
        using var harness = new ActionTestHarness();
        var old = harness.AddToHistory(new ClipboardSnapshot { Text = "old" });
        harness.AddToHistory(new ClipboardSnapshot { Text = "newer" });

        await new PasteAction().ExecuteAsync(old, harness.Context, ActionTestHarness.Ct);
        var captured = await harness.Monitor.CaptureNowAsync(ActionTestHarness.Ct);

        Assert.Null(captured);
        Assert.Equal(2, harness.History.Count);
    }

    [Fact]
    public async Task Execute_CancelledBeforeStart_DoesNotPaste()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "x" });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new PasteAction().ExecuteAsync(item, harness.Context, cancellation.Token));

        Assert.Equal(0, harness.Paste.Calls);
    }
}

public sealed class PasteAsPlainTextActionTests
{
    public static readonly TheoryData<string, bool> CanExecuteMatrix = new()
    {
        { "plain", false },
        { "whitespace", false },
        { "text+html", true },
        { "html-only", true },
        { "empty-text+html", true },
        { "rtf-only", false },  // RTF without text has no plain text to offer
        { "image", false },
        { "file", false },
        { "files", false },
        { "file+html", false },
        { "other", false },
        { "empty", false },
        { "link", false },
        { "image-link", false },
        { "text+image", false },
    };

    [Theory]
    [MemberData(nameof(CanExecuteMatrix))]
    public void CanExecute_OnlyForRichText(string kind, bool expected) =>
        Assert.Equal(expected, new PasteAsPlainTextAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public void CanExecute_AcceptsEveryPlatformsRichTextFormatName()
    {
        var action = new PasteAsPlainTextAction();
        string[] names = ["text/html", "HTML Format", "public.html", "Rich Text Format", "public.rtf", "text/rtf"];

        Assert.All(names, name => Assert.True(action.CanExecute(new ClipboardItem(new ClipboardSnapshot
        {
            Text = "x",
            RawFormats = [new RawClipboardFormat(name, Items.Utf8("<b>x</b>"))],
        }))));
    }

    [Fact]
    public async Task Execute_WithPlainText_WritesOnlyThatText_ThenPastes()
    {
        using var harness = new ActionTestHarness();
        var snapshot = new ClipboardSnapshot
        {
            Text = "Hello World",
            Image = [1, 2, 3],
            RawFormats = [new RawClipboardFormat("text/html", Items.Utf8("<b>Hello</b> World"))],
        };
        var item = harness.AddToHistory(snapshot);

        await new PasteAsPlainTextAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.Equal("Hello World", written.Text);
        Assert.Null(written.Image);
        Assert.Empty(written.Files);
        Assert.Empty(written.RawFormats);
        Assert.Equal(["write", "paste"], harness.EventLog);
        Assert.Equal(1, harness.Paste.Calls);
    }

    [Theory]
    [InlineData("html-only")]
    [InlineData("empty-text+html")]
    public async Task Execute_HtmlOnly_ExtractsTextFromTheHtml(string kind)
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(Items.Snapshot(kind));

        await new PasteAsPlainTextAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.Equal("Hello" + Environment.NewLine + "World", written.Text);
        Assert.Empty(written.RawFormats);
        Assert.Equal(1, harness.Paste.Calls);
    }

    [Fact]
    public async Task Execute_WindowsCfHtml_StripsHeaderEntitiesAndScripts()
    {
        using var harness = new ActionTestHarness();
        const string cfHtml = "Version:0.9\r\nStartHTML:00000097\r\nEndHTML:00000200\r\n"
            + "<html><body><script>alert(1)</script><p>Fish &amp; Chips</p></body></html>";
        var item = harness.AddToHistory(new ClipboardSnapshot
        {
            RawFormats = [new RawClipboardFormat("HTML Format", Items.Utf8(cfHtml))],
        });

        await new PasteAsPlainTextAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal("Fish & Chips", Assert.Single(harness.Backend.Writes).Text);
    }

    [Fact]
    public async Task Execute_UsesFirstRichFormat()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot
        {
            RawFormats =
            [
                new RawClipboardFormat("application/x-other", [9, 9]),
                new RawClipboardFormat("text/html", Items.Utf8("<p>first</p>")),
                new RawClipboardFormat("public.html", Items.Utf8("<p>second</p>")),
            ],
        });

        await new PasteAsPlainTextAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal("first", Assert.Single(harness.Backend.Writes).Text);
    }

    [Fact]
    public async Task Execute_DoesNotReorderHistory()
    {
        using var harness = new ActionTestHarness();
        var old = harness.AddToHistory(Items.Snapshot("text+html"));
        var newer = harness.AddToHistory(new ClipboardSnapshot { Text = "newer" });

        await new PasteAsPlainTextAction().ExecuteAsync(old, harness.Context, ActionTestHarness.Ct);

        Assert.Equal([newer.Id, old.Id], harness.History.Items.Select(item => item.Id));
    }

    /// <summary>
    /// The action promises "only the plain text of formatted (HTML or RTF) text". For an RTF-only item it must
    /// not paste the UI placeholder "Formatted text" into the user's document.
    /// </summary>
    [Fact]
    public async Task Execute_RtfOnly_DoesNotPasteThePreviewPlaceholder()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(Items.Snapshot("rtf-only"));
        var action = new PasteAsPlainTextAction();
        if (!action.CanExecute(item))
        {
            // Not offering the action at all is also an acceptable fix.
            return;
        }

        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var written = Assert.Single(harness.Backend.Writes);
        Assert.NotEqual("Formatted text", written.Text);
        Assert.Contains("Hello World", written.Text, StringComparison.Ordinal);
    }
}
