using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Persistence;

namespace Shapeshifter.Core.Tests.Persistence;

public sealed class HistoryPersistenceTests : IDisposable
{
    private readonly string parent = Directory.CreateTempSubdirectory("shapeshifter-history-").FullName;
    private bool enabled = true;

    private string Root => Path.Combine(parent, "History");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        try
        {
            Directory.Delete(parent, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private HistoryPersistence Create(ClipboardHistory history) => new(history, Root, () => enabled);

    private static ClipboardSnapshot Text(string text) => new() { Text = text };

    private static string[] Texts(ClipboardHistory history) => history.Items.Select(item => item.Snapshot.Text!).ToArray();

    private int SavedFolders() => Directory.Exists(Root) ? Directory.GetDirectories(Root).Length : 0;

    /// <summary>Saves the history the way the app does: start following it, wait for the sync, stop.</summary>
    private async Task SaveAsync(ClipboardHistory history)
    {
        var persistence = Create(history);
        persistence.Start();
        await persistence.FlushAsync();
        persistence.Stop();
    }

    /// <summary>Loads into a fresh history, as on the next start.</summary>
    private async Task<ClipboardHistory> RestartAsync(int maxItems = 50)
    {
        var restored = new ClipboardHistory(maxItems);
        await Create(restored).LoadAsync(Ct);
        return restored;
    }

    [Fact]
    public async Task History_SurvivesARestart_InTheSameOrder()
    {
        var history = new ClipboardHistory();
        history.Add(Text("first"));
        history.Add(Text("second"));
        history.Add(Text("third"));

        await SaveAsync(history);
        var restored = await RestartAsync();

        Assert.Equal(["third", "second", "first"], Texts(restored));
        Assert.Equal(history.Items.Select(item => item.Id), restored.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task CopyingAgain_MovesTheItemUp_AndKeepsWhenItWasCopied()
    {
        var history = new ClipboardHistory();
        var persistence = Create(history);
        persistence.Start();
        history.Add(Text("first"));
        history.Add(Text("second"));
        await persistence.FlushAsync();

        history.Add(Text("first"));
        await persistence.FlushAsync();
        persistence.Stop();
        var capturedAt = history.Items[0].CapturedAt;

        var restored = await RestartAsync();
        Assert.Equal(["first", "second"], Texts(restored));
        Assert.Equal(capturedAt, restored.Items[0].CapturedAt);
    }

    [Fact]
    public async Task RemovedItems_AreDeletedFromDisk()
    {
        var history = new ClipboardHistory();
        var persistence = Create(history);
        persistence.Start();
        var doomed = history.Add(Text("doomed"));
        history.Add(Text("kept"));
        await persistence.FlushAsync();
        Assert.Equal(2, SavedFolders());

        history.Remove(doomed.Id);
        await persistence.FlushAsync();
        persistence.Stop();

        Assert.Equal(1, SavedFolders());
        Assert.Equal(["kept"], Texts(await RestartAsync()));
    }

    [Fact]
    public async Task ClearingTheHistory_DeletesEverythingSaved()
    {
        var history = new ClipboardHistory();
        var persistence = Create(history);
        persistence.Start();
        history.Add(Text("a"));
        history.Add(Text("b"));
        await persistence.FlushAsync();

        history.Clear();
        await persistence.FlushAsync();

        Assert.Equal(0, SavedFolders());
        Assert.Empty(Texts(await RestartAsync()));
    }

    [Fact]
    public async Task PinnedItems_AreLeftToThePinnedStore()
    {
        var history = new ClipboardHistory();
        var persistence = Create(history);
        persistence.Start();
        var pinned = history.Add(Text("pinned"));
        history.Add(Text("plain"));
        await persistence.FlushAsync();
        Assert.Equal(2, SavedFolders());

        history.SetPinned(pinned.Id, true);
        await persistence.FlushAsync();
        Assert.Equal(1, SavedFolders());

        history.SetPinned(pinned.Id, false);
        await persistence.FlushAsync();
        Assert.Equal(2, SavedFolders());
    }

    [Fact]
    public async Task WhenOff_NothingIsSaved()
    {
        enabled = false;
        var history = new ClipboardHistory();
        history.Add(Text("secret"));

        await SaveAsync(history);

        Assert.False(Directory.Exists(Root));
    }

    [Fact]
    public async Task TurningItOff_DeletesWhatWasSaved_AndTurningItOnSavesTheHistoryAgain()
    {
        var history = new ClipboardHistory();
        var persistence = Create(history);
        persistence.Start();
        history.Add(Text("a"));
        await persistence.FlushAsync();
        Assert.Equal(1, SavedFolders());

        enabled = false;
        persistence.RequestSync();
        await persistence.FlushAsync();
        Assert.False(Directory.Exists(Root));

        enabled = true;
        persistence.RequestSync();
        await persistence.FlushAsync();
        Assert.Equal(1, SavedFolders());
    }

    [Fact]
    public async Task Load_WhenOff_RestoresNothing()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        await SaveAsync(history);

        enabled = false;

        Assert.Empty(Texts(await RestartAsync()));
    }

    [Fact]
    public async Task Load_GoesBelowWhatIsAlreadyThere_AndSkipsDuplicates()
    {
        var history = new ClipboardHistory();
        history.Add(Text("old"));
        history.Add(Text("same"));
        await SaveAsync(history);

        var restored = new ClipboardHistory();
        restored.Add(Text("same"));
        restored.Add(Text("new"));
        await Create(restored).LoadAsync(Ct);

        Assert.Equal(["new", "same", "old"], Texts(restored));
    }

    [Fact]
    public async Task Load_KeepsOnlyAsManyItemsAsTheHistoryHolds_AndTheNextSyncForgetsTheRest()
    {
        var history = new ClipboardHistory();
        foreach (var text in new[] { "1", "2", "3", "4" })
        {
            history.Add(Text(text));
        }

        await SaveAsync(history);
        var restored = await RestartAsync(maxItems: 2);
        Assert.Equal(["4", "3"], Texts(restored));

        await SaveAsync(restored);
        Assert.Equal(2, SavedFolders());
    }

    [Fact]
    public async Task Load_WithoutAnOrderFile_SortsNewestFirst()
    {
        var history = new ClipboardHistory();
        history.Add(Text("older")).CapturedAt = DateTimeOffset.UtcNow.AddHours(-1);
        history.Add(Text("newer"));
        await SaveAsync(history);
        File.Delete(Path.Combine(Root, "order.json"));

        Assert.Equal(["newer", "older"], Texts(await RestartAsync()));
    }

    [Fact]
    public async Task Load_WithAnUnreadableOrderFile_StillRestoresTheItems()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        await SaveAsync(history);
        await File.WriteAllTextAsync(Path.Combine(Root, "order.json"), "{ not json", Ct);

        Assert.Equal(["a"], Texts(await RestartAsync()));
    }
}
