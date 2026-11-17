using System.Text.Json;
using Serilog;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Persistence;

/// <summary>
/// Keeps the unpinned history on disk while "Remember history after restart" is on (issues #459 and #577), so it
/// survives restarts and reboots like the pinned items do. Off, nothing but the pinned items is written, and what was
/// saved before is deleted.
/// </summary>
/// <remarks>
/// Each item is a folder in the format of <see cref="PinnedItemStore"/>; <c>order.json</c> next to them lists the
/// items top first with when each was last copied. Every change of the history schedules one sync on a background
/// task, which writes the items that are not on disk yet, deletes the folders of items that are gone and rewrites
/// the order. Bursts of
/// changes coalesce into one sync.
/// </remarks>
public sealed class HistoryPersistence
{
    private const string OrderFileName = "order.json";

    private readonly ClipboardHistory history;
    private readonly string root;
    private readonly Func<bool> isEnabled;
    private readonly ILogger logger;
    private readonly PinnedItemStore store;
    private readonly Lock gate = new();
    private Task tail = Task.CompletedTask;
    private bool queued;
    private bool started;

    public HistoryPersistence(ClipboardHistory history, string root, Func<bool> isEnabled, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(isEnabled);
        this.history = history;
        this.root = root;
        this.isEnabled = isEnabled;
        this.logger = logger ?? Log.Logger;
        store = new PinnedItemStore(root, this.logger);
    }

    /// <summary>
    /// Adds the saved items to the history, below what it already holds, and returns how many there were. Does
    /// nothing while the setting is off (the next sync then deletes what is on disk).
    /// </summary>
    public async Task<int> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!isEnabled() || !Directory.Exists(root))
        {
            return 0;
        }

        var items = await store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        var order = await ReadOrderAsync(cancellationToken).ConfigureAwait(false);
        var byId = items.ToDictionary(item => item.Id);
        var positions = new Dictionary<Guid, int>();
        for (var i = 0; i < order.Count; i++)
        {
            positions.TryAdd(order[i].Id, i);
            if (byId.TryGetValue(order[i].Id, out var item))
            {
                item.CapturedAt = order[i].CapturedAt;
            }
        }

        // Listed items in their order, then any the order file does not know (a crash between writes), newest first.
        var sorted = items
            .OrderBy(item => positions.TryGetValue(item.Id, out var position) ? position : int.MaxValue)
            .ThenByDescending(item => item.CapturedAt)
            .ToArray();
        history.AddRestored(sorted);
        logger.Information("Restored {Count} history items", sorted.Length);
        return sorted.Length;
    }

    /// <summary>Follows the history from now on, and brings the disk up to date with it.</summary>
    public void Start()
    {
        lock (gate)
        {
            if (started)
            {
                return;
            }

            started = true;
        }

        history.Changed += OnHistoryChanged;
        RequestSync();
    }

    /// <summary>Stops following the history.</summary>
    public void Stop() => history.Changed -= OnHistoryChanged;

    /// <summary>Schedules a sync, for example after the setting changed.</summary>
    public void RequestSync()
    {
        lock (gate)
        {
            if (queued)
            {
                return;
            }

            queued = true;
            var previous = tail;
            tail = Task.Run(() => RunAfterAsync(previous));
        }
    }

    /// <summary>Completes once every scheduled sync has run.</summary>
    public Task FlushAsync()
    {
        lock (gate)
        {
            return tail;
        }
    }

    private void OnHistoryChanged(object? sender, HistoryChangedEventArgs e) => RequestSync();

    private async Task RunAfterAsync(Task previous)
    {
        await previous.ConfigureAwait(false);
        lock (gate)
        {
            // Changes from here on need another sync; those before are covered by this one.
            queued = false;
        }

        try
        {
            await SyncAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.Warning(exception, "Could not save the history to {Folder}", root);
        }
    }

    private async Task SyncAsync()
    {
        if (!isEnabled())
        {
            await DeleteAllAsync().ConfigureAwait(false);
            return;
        }

        // What is on disk is what was saved: a folder only appears once its item is completely written.
        var current = history.Items.Where(item => !item.IsPinned).ToArray();
        var currentIds = current.Select(item => item.Id).ToHashSet();
        var onDisk = Directory.Exists(root)
            ? Directory.EnumerateDirectories(root)
                .Select(folder => Guid.TryParseExact(Path.GetFileName(folder), "N", out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToHashSet()
            : [];

        foreach (var id in onDisk.Where(id => !currentIds.Contains(id)))
        {
            await store.DeleteAsync(id).ConfigureAwait(false);
        }

        foreach (var item in current.Where(item => !onDisk.Contains(item.Id)))
        {
            await store.SaveAsync(item).ConfigureAwait(false);
        }

        Directory.CreateDirectory(root);
        var order = current.Select(item => new OrderEntry { Id = item.Id, CapturedAt = item.CapturedAt }).ToList();
        var path = Path.Combine(root, OrderFileName);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(order)).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }

    private Task DeleteAllAsync()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
            logger.Information("Deleted the saved history");
        }

        return Task.CompletedTask;
    }

    private async Task<List<OrderEntry>> ReadOrderAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, OrderFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<OrderEntry>>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)) ?? [];
        }
        catch (JsonException exception)
        {
            logger.Warning(exception, "Ignoring the unreadable history order in {File}", path);
            return [];
        }
    }

    private sealed class OrderEntry
    {
        public Guid Id { get; set; }

        public DateTimeOffset CapturedAt { get; set; }
    }
}
