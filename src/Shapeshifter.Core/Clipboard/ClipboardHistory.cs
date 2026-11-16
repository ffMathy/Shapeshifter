using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Clipboard;

public enum HistoryChangeKind
{
    Added,
    Moved,
    Removed,
    PinChanged,
    Reset,
}

public sealed class HistoryChangedEventArgs(HistoryChangeKind kind, ClipboardItem? item) : EventArgs
{
    public HistoryChangeKind Kind { get; } = kind;

    public ClipboardItem? Item { get; } = item;
}

/// <summary>
/// The ordered, thread-safe clipboard history. Pinned items are kept at the top (in pin order), followed by
/// the unpinned items from newest to oldest. The newest unpinned item normally mirrors the current clipboard.
/// </summary>
public sealed class ClipboardHistory
{
    private readonly Lock gate = new();
    private readonly List<ClipboardItem> items = [];
    private int maxUnpinnedItems;

    public ClipboardHistory(int maxUnpinnedItems = 50)
    {
        MaxUnpinnedItems = maxUnpinnedItems;
    }

    public event EventHandler<HistoryChangedEventArgs>? Changed;

    /// <summary>Unpinned items beyond this count are dropped (oldest first).</summary>
    public int MaxUnpinnedItems
    {
        get => maxUnpinnedItems;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            List<ClipboardItem> removed;
            lock (gate)
            {
                maxUnpinnedItems = value;
                removed = TrimLocked();
            }

            foreach (var item in removed)
            {
                Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Removed, item));
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return items.Count;
            }
        }
    }

    /// <summary>A point-in-time copy of the history, top first.</summary>
    public IReadOnlyList<ClipboardItem> Items
    {
        get
        {
            lock (gate)
            {
                return items.ToArray();
            }
        }
    }

    /// <summary>Index of the newest unpinned item in <paramref name="snapshot"/>, or 0.</summary>
    public static int GetDefaultIndex(IReadOnlyList<ClipboardItem> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        for (var i = 0; i < snapshot.Count; i++)
        {
            if (!snapshot[i].IsPinned)
            {
                return i;
            }
        }

        return 0;
    }

    public ClipboardItem? Find(Guid id)
    {
        lock (gate)
        {
            return items.Find(item => item.Id == id);
        }
    }

    /// <summary>
    /// Adds a captured snapshot as the newest item. If an item with the same content already exists, that
    /// item is moved to the top instead (pinned duplicates stay where they are) and returned.
    /// </summary>
    public ClipboardItem Add(ClipboardSnapshot snapshot, out bool isNew) => Add(snapshot, source: null, out isNew);

    /// <inheritdoc cref="Add(ClipboardSnapshot, out bool)"/>
    /// <param name="source">Where the snapshot was copied from; kept only when a new item is created.</param>
    public ClipboardItem Add(ClipboardSnapshot snapshot, ClipboardSource? source, out bool isNew)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ClipboardItem result;
        List<ClipboardItem> removed;
        HistoryChangeKind kind;
        lock (gate)
        {
            var existing = items.Find(item => item.Snapshot.Fingerprint == snapshot.Fingerprint);
            if (existing is not null)
            {
                isNew = false;
                result = existing;

                // Content already on the clipboard at startup has no known source; a later copy of it does.
                existing.Source ??= source;
                if (existing.IsPinned)
                {
                    return existing;
                }

                items.Remove(existing);
                items.Insert(PinnedCountLocked(), existing);
                kind = HistoryChangeKind.Moved;
                removed = [];
            }
            else
            {
                isNew = true;
                result = new ClipboardItem(snapshot, source: source);
                items.Insert(PinnedCountLocked(), result);
                kind = HistoryChangeKind.Added;
                removed = TrimLocked();
            }
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(kind, result));
        foreach (var item in removed)
        {
            Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Removed, item));
        }

        return result;
    }

    public ClipboardItem Add(ClipboardSnapshot snapshot) => Add(snapshot, out _);

    /// <summary>Adds an item restored from disk as pinned, after the existing pinned items.</summary>
    public void AddPinned(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (gate)
        {
            if (items.Any(existing => existing.Id == item.Id))
            {
                return;
            }

            item.IsPinned = true;
            items.Insert(PinnedCountLocked(), item);
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Added, item));
    }

    /// <summary>Moves an unpinned item to the top of the unpinned section (it becomes the newest).</summary>
    public bool MoveToTop(Guid id)
    {
        ClipboardItem? item;
        lock (gate)
        {
            item = items.Find(candidate => candidate.Id == id);
            if (item is null || item.IsPinned)
            {
                return false;
            }

            items.Remove(item);
            items.Insert(PinnedCountLocked(), item);
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Moved, item));
        return true;
    }

    public bool Remove(Guid id)
    {
        ClipboardItem? item;
        lock (gate)
        {
            item = items.Find(candidate => candidate.Id == id);
            if (item is null)
            {
                return false;
            }

            items.Remove(item);
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Removed, item));
        return true;
    }

    public bool SetPinned(Guid id, bool pinned)
    {
        ClipboardItem? item;
        List<ClipboardItem> removed = [];
        lock (gate)
        {
            item = items.Find(candidate => candidate.Id == id);
            if (item is null || item.IsPinned == pinned)
            {
                return false;
            }

            items.Remove(item);
            if (pinned)
            {
                items.Insert(PinnedCountLocked(), item);
                item.IsPinned = true;
            }
            else
            {
                item.IsPinned = false;
                items.Insert(PinnedCountLocked(), item);
                removed = TrimLocked();
            }
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.PinChanged, item));
        foreach (var dropped in removed)
        {
            Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Removed, dropped));
        }

        return true;
    }

    public void Clear(bool includePinned = false)
    {
        lock (gate)
        {
            items.RemoveAll(item => includePinned || !item.IsPinned);
        }

        Changed?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Reset, null));
    }

    private int PinnedCountLocked()
    {
        var count = 0;
        while (count < items.Count && items[count].IsPinned)
        {
            count++;
        }

        return count;
    }

    private List<ClipboardItem> TrimLocked()
    {
        var removed = new List<ClipboardItem>();
        var unpinned = items.Count(item => !item.IsPinned);
        for (var i = items.Count - 1; i >= 0 && unpinned > maxUnpinnedItems; i--)
        {
            if (!items[i].IsPinned)
            {
                removed.Add(items[i]);
                items.RemoveAt(i);
                unpinned--;
            }
        }

        return removed;
    }
}
