using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Actions;

public sealed class TogglePinActionTests
{
    [Theory]
    [MemberData(nameof(Items.AllKinds), MemberType = typeof(Items))]
    public void CanExecute_IsTrueForEveryKind(string kind) =>
        Assert.True(new TogglePinAction().CanExecute(Items.ByName(kind)));

    [Fact]
    public void GetTitle_DependsOnPinState()
    {
        var history = new ClipboardHistory();
        var item = history.Add(new ClipboardSnapshot { Text = "x" });
        var action = new TogglePinAction();

        Assert.Equal("Pin to top", action.GetTitle(item));
        history.SetPinned(item.Id, pinned: true);
        Assert.Equal("Unpin", action.GetTitle(item));
    }

    [Fact]
    public async Task Execute_Unpinned_PinsAndPersists()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot
        {
            Text = "keep me",
            RawFormats = [new RawClipboardFormat("text/html", Items.Utf8("<b>keep me</b>"))],
        });
        var newer = harness.AddToHistory(new ClipboardSnapshot { Text = "newer" });

        await new TogglePinAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.True(item.IsPinned);
        Assert.Equal([item.Id, newer.Id], harness.History.Items.Select(i => i.Id));
        Assert.True(Directory.Exists(Path.Combine(harness.Paths.PinnedDirectory, item.Id.ToString("N"))));

        var stored = Assert.Single(await harness.PinnedItems.LoadAllAsync(ActionTestHarness.Ct));
        Assert.Equal(item.Id, stored.Id);
        Assert.Equal(item.CapturedAt, stored.CapturedAt);
        Assert.Equal("keep me", stored.Snapshot.Text);
        Assert.Equal(item.Snapshot.Fingerprint, stored.Snapshot.Fingerprint);
        var raw = Assert.Single(stored.Snapshot.RawFormats);
        Assert.Equal("text/html", raw.Name);
        Assert.Equal(Items.Utf8("<b>keep me</b>"), raw.Data);

        Assert.Empty(harness.Backend.Writes);
        Assert.Equal(0, harness.Paste.Calls);
    }

    [Fact]
    public async Task Execute_Pinned_UnpinsAndDeletesFromDisk()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "keep me" });
        var action = new TogglePinAction();
        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.False(item.IsPinned);
        Assert.Same(item, Assert.Single(harness.History.Items));
        Assert.Empty(await harness.PinnedItems.LoadAllAsync(ActionTestHarness.Ct));
        Assert.False(Directory.Exists(Path.Combine(harness.Paths.PinnedDirectory, item.Id.ToString("N"))));
    }

    [Fact]
    public async Task Execute_Unpin_MovesItemToTopOfUnpinnedSection()
    {
        using var harness = new ActionTestHarness();
        var other = harness.AddToHistory(new ClipboardSnapshot { Text = "other pinned" });
        var item = harness.AddToHistory(new ClipboardSnapshot { Text = "target" });
        var action = new TogglePinAction();
        await action.ExecuteAsync(other, harness.Context, ActionTestHarness.Ct);
        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);
        var unpinned = harness.AddToHistory(new ClipboardSnapshot { Text = "unpinned" });

        await action.ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        Assert.Equal([other.Id, item.Id, unpinned.Id], harness.History.Items.Select(i => i.Id));
        var stillPinned = Assert.Single(await harness.PinnedItems.LoadAllAsync(ActionTestHarness.Ct));
        Assert.Equal(other.Id, stillPinned.Id);
    }

    [Fact]
    public async Task Execute_PinnedItemsSurviveARestart()
    {
        using var harness = new ActionTestHarness();
        var item = harness.AddToHistory(new ClipboardSnapshot { Image = [1, 2, 3, 4] });
        await new TogglePinAction().ExecuteAsync(item, harness.Context, ActionTestHarness.Ct);

        var restartedHistory = new ClipboardHistory();
        var restartedStore = new Shapeshifter.Core.Persistence.PinnedItemStore(harness.Paths.PinnedDirectory);
        foreach (var loaded in await restartedStore.LoadAllAsync(ActionTestHarness.Ct))
        {
            restartedHistory.AddPinned(loaded);
        }

        var restored = Assert.Single(restartedHistory.Items);
        Assert.True(restored.IsPinned);
        Assert.Equal(item.Id, restored.Id);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, restored.Snapshot.Image);
    }
}
