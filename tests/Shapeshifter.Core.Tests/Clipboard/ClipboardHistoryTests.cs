using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class ClipboardHistoryTests
{
    private static ClipboardSnapshot Text(string text) => new() { Text = text };

    private static string[] Texts(ClipboardHistory history) => history.Items.Select(i => i.Snapshot.Text!).ToArray();

    private static List<HistoryChangedEventArgs> Record(ClipboardHistory history)
    {
        var events = new List<HistoryChangedEventArgs>();
        history.Changed += (_, e) => events.Add(e);
        return events;
    }

    [Fact]
    public void NewHistory_IsEmpty()
    {
        var history = new ClipboardHistory();

        Assert.Equal(0, history.Count);
        Assert.Empty(history.Items);
        Assert.Equal(50, history.MaxUnpinnedItems);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsMaxBelowOne(int max) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClipboardHistory(max));

    [Fact]
    public void MaxUnpinnedItems_RejectsValuesBelowOne()
    {
        var history = new ClipboardHistory(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => history.MaxUnpinnedItems = 0);
        Assert.Equal(3, history.MaxUnpinnedItems);
    }

    [Fact]
    public void Add_PutsNewestItemFirst()
    {
        var history = new ClipboardHistory();

        history.Add(Text("a"));
        history.Add(Text("b"));
        history.Add(Text("c"));

        Assert.Equal(["c", "b", "a"], Texts(history));
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void Add_NewContent_ReturnsNewItemAndReportsIsNew()
    {
        var history = new ClipboardHistory();
        var snapshot = Text("a");

        var item = history.Add(snapshot, out var isNew);

        Assert.True(isNew);
        Assert.Same(snapshot, item.Snapshot);
        Assert.False(item.IsPinned);
        Assert.Same(item, Assert.Single(history.Items));
    }

    [Fact]
    public void Add_NullSnapshot_Throws()
    {
        var history = new ClipboardHistory();

        Assert.Throws<ArgumentNullException>(() => history.Add(null!));
    }

    [Fact]
    public void Add_DuplicateContent_MovesExistingItemToTop()
    {
        var history = new ClipboardHistory();
        var first = history.Add(Text("a"));
        history.Add(Text("b"));
        history.Add(Text("c"));

        var result = history.Add(Text("a"), out var isNew);

        Assert.False(isNew);
        Assert.Same(first, result);
        Assert.Equal(["a", "c", "b"], Texts(history));
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void Add_DuplicateWithoutSource_TakesTheSourceOfTheNewCopy()
    {
        var history = new ClipboardHistory();
        var item = history.Add(Text("a"), source: null, out _);
        var first = new ClipboardSource("Browser", "Page – Browser", null);
        var second = new ClipboardSource("Editor", "File – Editor", null);

        history.Add(Text("a"), first, out var isNew);
        history.Add(Text("a"), second, out _);

        Assert.False(isNew);
        Assert.Same(first, item.Source);
    }

    [Fact]
    public void Add_DuplicateIsDetectedByFingerprintNotReference()
    {
        var history = new ClipboardHistory();
        var first = history.Add(new ClipboardSnapshot
        {
            Text = "same",
            RawFormats = [new RawClipboardFormat("x-volatile/timestamp", [1])],
        });
        history.Add(Text("other"));

        var result = history.Add(new ClipboardSnapshot
        {
            Text = "same",
            RawFormats = [new RawClipboardFormat("x-volatile/timestamp", [2])],
        }, out var isNew);

        Assert.False(isNew);
        Assert.Same(first, result);
        Assert.Equal(2, history.Count);
        Assert.Same(first, history.Items[0]);
    }

    [Fact]
    public void Add_DuplicateOfTopItem_RaisesMovedAndKeepsOrder()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        var top = history.Add(Text("b"));
        var events = Record(history);

        history.Add(Text("b"));

        Assert.Equal(["b", "a"], Texts(history));
        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Moved, change.Kind);
        Assert.Same(top, change.Item);
    }

    [Fact]
    public void Add_NewItemGoesBelowPinnedItems()
    {
        var history = new ClipboardHistory();
        var pinned = history.Add(Text("pinned"));
        history.SetPinned(pinned.Id, true);

        history.Add(Text("new"));

        Assert.Equal(["pinned", "new"], Texts(history));
        Assert.Equal(1, ClipboardHistory.GetDefaultIndex(history.Items));
    }

    [Fact]
    public void Add_DuplicateOfPinnedItem_StaysInPlaceAndRaisesNoEvent()
    {
        var history = new ClipboardHistory();
        var p1 = history.Add(Text("p1"));
        var p2 = history.Add(Text("p2"));
        history.SetPinned(p1.Id, true);
        history.SetPinned(p2.Id, true);
        history.Add(Text("u"));
        var before = Texts(history);
        var events = Record(history);

        var result = history.Add(Text("p2"), out var isNew);

        Assert.False(isNew);
        Assert.Same(p2, result);
        Assert.True(result.IsPinned);
        Assert.Equal(before, Texts(history));
        Assert.Empty(events);
    }

    [Fact]
    public void Add_DuplicateUnpinnedItem_MovesBelowPinned()
    {
        var history = new ClipboardHistory();
        var old = history.Add(Text("old"));
        var pinned = history.Add(Text("pinned"));
        history.SetPinned(pinned.Id, true);
        history.Add(Text("newer"));

        history.Add(Text("old"));

        Assert.Equal(["pinned", "old", "newer"], Texts(history));
        Assert.False(old.IsPinned);
    }

    [Fact]
    public void Add_RaisesAddedWithItem()
    {
        var history = new ClipboardHistory();
        var events = Record(history);

        var item = history.Add(Text("a"));

        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Added, change.Kind);
        Assert.Same(item, change.Item);
    }

    [Fact]
    public void Add_BeyondMax_DropsOldestUnpinnedAndRaisesRemovedAfterAdded()
    {
        var history = new ClipboardHistory(2);
        var oldest = history.Add(Text("a"));
        history.Add(Text("b"));
        var events = Record(history);

        var added = history.Add(Text("c"));

        Assert.Equal(["c", "b"], Texts(history));
        Assert.Null(history.Find(oldest.Id));
        Assert.Collection(
            events,
            e =>
            {
                Assert.Equal(HistoryChangeKind.Added, e.Kind);
                Assert.Same(added, e.Item);
            },
            e =>
            {
                Assert.Equal(HistoryChangeKind.Removed, e.Kind);
                Assert.Same(oldest, e.Item);
            });
    }

    [Fact]
    public void Trimming_NeverDropsPinnedItems()
    {
        var history = new ClipboardHistory(1);
        var p1 = history.Add(Text("p1"));
        history.SetPinned(p1.Id, true);
        var p2 = history.Add(Text("p2"));
        history.SetPinned(p2.Id, true);

        for (var i = 0; i < 10; i++)
        {
            history.Add(Text($"u{i}"));
        }

        Assert.Equal(["p1", "p2", "u9"], Texts(history));
    }

    [Fact]
    public void Trimming_IsBasedOnUnpinnedCountOnly()
    {
        var history = new ClipboardHistory(2);
        for (var i = 0; i < 3; i++)
        {
            var p = history.Add(Text($"p{i}"));
            history.SetPinned(p.Id, true);
        }

        history.Add(Text("u1"));
        history.Add(Text("u2"));

        Assert.Equal(5, history.Count);
        Assert.Equal(["p0", "p1", "p2", "u2", "u1"], Texts(history));
    }

    [Fact]
    public void LoweringMax_TrimsOldestAndRaisesRemovedForEach()
    {
        var history = new ClipboardHistory(10);
        var items = Enumerable.Range(0, 5).Select(i => history.Add(Text($"u{i}"))).ToArray();
        var pinned = history.Add(Text("p"));
        history.SetPinned(pinned.Id, true);
        var events = Record(history);

        history.MaxUnpinnedItems = 2;

        Assert.Equal(["p", "u4", "u3"], Texts(history));
        Assert.All(events, e => Assert.Equal(HistoryChangeKind.Removed, e.Kind));
        Assert.Equal([items[0], items[1], items[2]], events.Select(e => e.Item));
    }

    [Fact]
    public void RaisingMax_DoesNotRaiseEvents()
    {
        var history = new ClipboardHistory(2);
        history.Add(Text("a"));
        history.Add(Text("b"));
        var events = Record(history);

        history.MaxUnpinnedItems = 5;

        Assert.Empty(events);
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void SetPinned_PinsInPinOrderAtTop()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));
        var b = history.Add(Text("b"));
        var c = history.Add(Text("c"));

        Assert.True(history.SetPinned(a.Id, true));
        Assert.True(history.SetPinned(c.Id, true));

        Assert.Equal(["a", "c", "b"], Texts(history));
        Assert.True(a.IsPinned);
        Assert.True(c.IsPinned);
        Assert.False(b.IsPinned);
    }

    [Fact]
    public void SetPinned_RaisesPinChanged()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));
        var events = Record(history);

        history.SetPinned(a.Id, true);
        history.SetPinned(a.Id, false);

        Assert.Equal(2, events.Count);
        Assert.All(events, e =>
        {
            Assert.Equal(HistoryChangeKind.PinChanged, e.Kind);
            Assert.Same(a, e.Item);
        });
    }

    [Fact]
    public void SetPinned_SameStateOrUnknownId_ReturnsFalseWithoutEvent()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));
        var events = Record(history);

        Assert.False(history.SetPinned(a.Id, false));
        Assert.False(history.SetPinned(Guid.NewGuid(), true));
        history.SetPinned(a.Id, true);
        events.Clear();
        Assert.False(history.SetPinned(a.Id, true));

        Assert.Empty(events);
    }

    [Fact]
    public void Unpin_PlacesItemAtTopOfUnpinnedSection()
    {
        var history = new ClipboardHistory();
        var p1 = history.Add(Text("p1"));
        var p2 = history.Add(Text("p2"));
        history.SetPinned(p1.Id, true);
        history.SetPinned(p2.Id, true);
        history.Add(Text("u1"));
        history.Add(Text("u2"));

        Assert.True(history.SetPinned(p1.Id, false));

        Assert.Equal(["p2", "p1", "u2", "u1"], Texts(history));
        Assert.False(p1.IsPinned);
        Assert.Equal(1, ClipboardHistory.GetDefaultIndex(history.Items));
    }

    [Fact]
    public void Unpin_WhenOverMax_TrimsOldestUnpinnedAndRaisesRemoved()
    {
        var history = new ClipboardHistory(2);
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);
        var oldest = history.Add(Text("u1"));
        history.Add(Text("u2"));
        var events = Record(history);

        history.SetPinned(p.Id, false);

        Assert.Equal(["p", "u2"], Texts(history));
        Assert.Collection(
            events,
            e =>
            {
                Assert.Equal(HistoryChangeKind.PinChanged, e.Kind);
                Assert.Same(p, e.Item);
            },
            e =>
            {
                Assert.Equal(HistoryChangeKind.Removed, e.Kind);
                Assert.Same(oldest, e.Item);
            });
    }

    [Fact]
    public void MoveToTop_MovesUnpinnedItemBelowPinned()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);
        history.Add(Text("b"));
        var events = Record(history);

        Assert.True(history.MoveToTop(a.Id));

        Assert.Equal(["p", "a", "b"], Texts(history));
        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Moved, change.Kind);
        Assert.Same(a, change.Item);
    }

    [Fact]
    public void MoveToTop_PinnedOrUnknown_ReturnsFalseWithoutEvent()
    {
        var history = new ClipboardHistory();
        var p1 = history.Add(Text("p1"));
        var p2 = history.Add(Text("p2"));
        history.SetPinned(p1.Id, true);
        history.SetPinned(p2.Id, true);
        var events = Record(history);

        Assert.False(history.MoveToTop(p2.Id));
        Assert.False(history.MoveToTop(Guid.NewGuid()));

        Assert.Equal(["p1", "p2"], Texts(history));
        Assert.Empty(events);
    }

    [Fact]
    public void Remove_RemovesItemAndRaisesRemoved()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        var b = history.Add(Text("b"));
        history.Add(Text("c"));
        var events = Record(history);

        Assert.True(history.Remove(b.Id));

        Assert.Equal(["c", "a"], Texts(history));
        Assert.Null(history.Find(b.Id));
        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Removed, change.Kind);
        Assert.Same(b, change.Item);
    }

    [Fact]
    public void Remove_PinnedItem_IsAllowed()
    {
        var history = new ClipboardHistory();
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);

        Assert.True(history.Remove(p.Id));
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Remove_UnknownId_ReturnsFalseWithoutEvent()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        var events = Record(history);

        Assert.False(history.Remove(Guid.NewGuid()));

        Assert.Equal(1, history.Count);
        Assert.Empty(events);
    }

    [Fact]
    public void RemovedContent_CanBeAddedAgainAsNew()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));
        history.Remove(a.Id);

        var again = history.Add(Text("a"), out var isNew);

        Assert.True(isNew);
        Assert.NotEqual(a.Id, again.Id);
    }

    [Fact]
    public void Clear_KeepsPinnedByDefault()
    {
        var history = new ClipboardHistory();
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);
        history.Add(Text("u1"));
        history.Add(Text("u2"));
        var events = Record(history);

        history.Clear();

        Assert.Same(p, Assert.Single(history.Items));
        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Reset, change.Kind);
        Assert.Null(change.Item);
    }

    [Fact]
    public void Clear_IncludePinned_RemovesEverything()
    {
        var history = new ClipboardHistory();
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);
        history.Add(Text("u"));
        var events = Record(history);

        history.Clear(includePinned: true);

        Assert.Equal(0, history.Count);
        Assert.Equal(HistoryChangeKind.Reset, Assert.Single(events).Kind);
    }

    [Fact]
    public void AddPinned_AppendsAfterExistingPinnedAndMarksPinned()
    {
        var history = new ClipboardHistory();
        var p1 = history.Add(Text("p1"));
        history.SetPinned(p1.Id, true);
        history.Add(Text("u"));
        var restored = new ClipboardItem(Text("restored"));
        var events = Record(history);

        history.AddPinned(restored);

        Assert.True(restored.IsPinned);
        Assert.Equal(["p1", "restored", "u"], Texts(history));
        var change = Assert.Single(events);
        Assert.Equal(HistoryChangeKind.Added, change.Kind);
        Assert.Same(restored, change.Item);
    }

    [Fact]
    public void AddPinned_SameIdTwice_IsIgnored()
    {
        var history = new ClipboardHistory();
        var restored = new ClipboardItem(Text("restored"));
        history.AddPinned(restored);
        var events = Record(history);

        history.AddPinned(restored);

        Assert.Single(history.Items);
        Assert.Empty(events);
    }

    [Fact]
    public void AddPinned_IsNotTrimmed()
    {
        var history = new ClipboardHistory(1);

        for (var i = 0; i < 5; i++)
        {
            history.AddPinned(new ClipboardItem(Text($"p{i}")));
        }

        Assert.Equal(5, history.Count);
    }

    [Fact]
    public void AddPinned_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ClipboardHistory().AddPinned(null!));

    [Fact]
    public void Find_ReturnsItemById()
    {
        var history = new ClipboardHistory();
        var a = history.Add(Text("a"));

        Assert.Same(a, history.Find(a.Id));
        Assert.Null(history.Find(Guid.NewGuid()));
    }

    [Fact]
    public void Items_IsPointInTimeCopy()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        var snapshot = history.Items;

        history.Add(Text("b"));

        Assert.Single(snapshot);
        Assert.Equal(2, history.Items.Count);
    }

    [Fact]
    public void GetDefaultIndex_EdgeCases()
    {
        Assert.Throws<ArgumentNullException>(() => ClipboardHistory.GetDefaultIndex(null!));
        Assert.Equal(0, ClipboardHistory.GetDefaultIndex([]));

        var history = new ClipboardHistory();
        var p = history.Add(Text("p"));
        history.SetPinned(p.Id, true);
        Assert.Equal(0, ClipboardHistory.GetDefaultIndex(history.Items));

        history.Add(Text("u"));
        Assert.Equal(1, ClipboardHistory.GetDefaultIndex(history.Items));
    }

    [Fact]
    public void GetDefaultIndex_NoPinned_IsZero()
    {
        var history = new ClipboardHistory();
        history.Add(Text("a"));
        history.Add(Text("b"));

        Assert.Equal(0, ClipboardHistory.GetDefaultIndex(history.Items));
    }

    [Fact]
    public void ChangedHandler_CanReadHistoryWithoutDeadlock()
    {
        var history = new ClipboardHistory();
        var counts = new List<int>();
        history.Changed += (_, _) => counts.Add(history.Count);

        history.Add(Text("a"));
        history.Add(Text("b"));

        Assert.Equal([1, 2], counts);
    }

    [Fact]
    public async Task ParallelAdds_KeepInvariants()
    {
        const int Max = 20;
        var history = new ClipboardHistory(Max);
        var p = history.Add(Text("pinned"));
        history.SetPinned(p.Id, true);
        var added = 0;
        var removed = 0;
        history.Changed += (_, e) =>
        {
            if (e.Kind == HistoryChangeKind.Added)
            {
                Interlocked.Increment(ref added);
            }
            else if (e.Kind == HistoryChangeKind.Removed)
            {
                Interlocked.Increment(ref removed);
            }
        };

        var tasks = Enumerable.Range(0, 8).Select(t => Task.Run(
            () =>
            {
                for (var i = 0; i < 250; i++)
                {
                    // Every fourth value collides with another thread's value to exercise the move path.
                    history.Add(Text(i % 4 == 0 ? $"shared{i}" : $"t{t}-{i}"));
                    _ = history.Items;
                }
            },
            TestContext.Current.CancellationToken));
        await Task.WhenAll(tasks);

        var items = history.Items;
        Assert.Equal(Max + 1, items.Count);
        Assert.Same(p, items[0]);
        Assert.All(items.Skip(1), i => Assert.False(i.IsPinned));
        Assert.Equal(items.Count, items.Select(i => i.Snapshot.Fingerprint).Distinct().Count());
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
        Assert.Equal(Max, added - removed);
    }
}
