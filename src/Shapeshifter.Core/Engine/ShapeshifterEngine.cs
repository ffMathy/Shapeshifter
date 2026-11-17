using System.Threading.Channels;
using Serilog;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Engine;

/// <summary>An action as the picker displays it.</summary>
public sealed record PickerActionView(string Title, string Description);

/// <summary>What the picker displays.</summary>
/// <remarks>
/// While searching, <see cref="SearchQuery"/> is what the user typed and <see cref="Items"/> the items that match it,
/// which may be none.
/// </remarks>
public sealed record PickerViewState(
    IReadOnlyList<ClipboardItem> Items,
    int SelectedIndex,
    IReadOnlyList<PickerActionView> Actions,
    int SelectedActionIndex,
    PickerPane Pane)
{
    /// <summary>What the user typed in the search box, or <see langword="null"/> while not searching.</summary>
    public string? SearchQuery { get; init; }

    public bool IsSearching => SearchQuery is not null;
}

/// <summary>
/// The picker UI. Implementations marshal to their UI thread. The picker never takes the keyboard focus, except while
/// the user searches the history (<see cref="BeginSearchAsync"/>), until it is hidden.
/// </summary>
public interface IPickerPresenter
{
    Task ShowAsync(PickerViewState state);

    Task UpdateAsync(PickerViewState state);

    Task HideAsync();

    /// <summary>
    /// Switches the visible picker to searching: it takes the keyboard focus and shows a search box. Everything the user
    /// does in it is reported to <paramref name="onInput"/>, which returns whether the key was used (the search box
    /// should not handle it as well). The search ends with <see cref="HideAsync"/>.
    /// </summary>
    Task BeginSearchAsync(PickerViewState state, Func<PickerSearchInput, bool> onInput);
}

/// <summary>Shows the short "copied" notification.</summary>
public interface ICopyNotifier
{
    void NotifyCopied(ClipboardItem item, int historyCount);
}

/// <summary>Coordinates the clipboard monitor, the keyboard hook, the gesture and the picker.</summary>
public sealed class ShapeshifterEngine : IAsyncDisposable
{
    private readonly ClipboardHistory history;
    private readonly ClipboardMonitor monitor;
    private readonly IKeyboardHook keyboardHook;
    private readonly ActionCatalog actions;
    private readonly ActionContext actionContext;
    private readonly IPickerPresenter picker;
    private readonly ICopyNotifier? copyNotifier;
    private readonly IFocusedWindowTracker? focusTracker;
    private readonly ILogger logger;
    private readonly TimeProvider time;
    private readonly PasteGestureStateMachine gesture;
    private readonly Lock gestureGate = new();
    private readonly Channel<GestureEffect> effects = Channel.CreateUnbounded<GestureEffect>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Dictionary<Guid, (ClipboardItem Item, IReadOnlyList<IClipboardAction> Actions)> session = [];
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, IReadOnlyList<IClipboardAction>> availableActions = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> searchTexts = new();
    private readonly long startTimestamp;
    private readonly CancellationTokenSource shutdown = new();
    private ITimer? tickTimer;
    private Task? effectLoop;
    private bool navigationCaptured;

    /// <summary>Set (under <see cref="gestureGate"/>) from the moment a search begins until it ends.</summary>
    private bool searching;
    private PickerSearch? search;

    /// <summary>The window that had the focus when the search began; only used by the effect loop.</summary>
    private FocusedWindow? focusBeforeSearch;
    private int disposed;

    public ShapeshifterEngine(
        ClipboardHistory history,
        ClipboardMonitor monitor,
        IKeyboardHook keyboardHook,
        ActionCatalog actions,
        ActionContext actionContext,
        IPickerPresenter picker,
        ICopyNotifier? copyNotifier = null,
        ILogger? logger = null,
        TimeProvider? time = null,
        IFocusedWindowTracker? focusTracker = null)
    {
        this.history = history;
        this.monitor = monitor;
        this.keyboardHook = keyboardHook;
        this.actions = actions;
        this.actionContext = actionContext;
        this.picker = picker;
        this.copyNotifier = copyNotifier;
        this.focusTracker = focusTracker;
        this.logger = logger ?? Log.Logger;
        this.time = time ?? TimeProvider.System;
        startTimestamp = this.time.GetTimestamp();
        gesture = new PasteGestureStateMachine(CreateSession);
        history.Changed += OnHistoryChanged;
        foreach (var item in history.Items)
        {
            availableActions.TryAdd(item.Id, actions.GetAvailable(item));
        }
    }

    /// <summary>Raised after every processed gesture effect. Useful for diagnostics and tests.</summary>
    public event EventHandler<GestureEffect>? EffectProcessed;

    public TimeSpan PasteDelay
    {
        get
        {
            lock (gestureGate)
            {
                return gesture.ShowDelay;
            }
        }
        set
        {
            lock (gestureGate)
            {
                gesture.ShowDelay = value;
            }
        }
    }

    public bool IsGestureActive
    {
        get
        {
            lock (gestureGate)
            {
                return gesture.IsActive;
            }
        }
    }

    /// <summary>Whether the user is searching the history in the picker.</summary>
    public bool IsSearching
    {
        get
        {
            lock (gestureGate)
            {
                return searching;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        effectLoop = Task.Run(ProcessEffectsAsync, CancellationToken.None);
        monitor.Captured += OnCaptured;
        monitor.Start();
        await monitor.CaptureNowAsync(cancellationToken).ConfigureAwait(false);

        keyboardHook.KeyHandler = OnKey;
        await keyboardHook.StartAsync(cancellationToken).ConfigureAwait(false);
        tickTimer = time.CreateTimer(_ => OnTick(), null, TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(25));
        logger.Information("Shapeshifter engine started");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        keyboardHook.KeyHandler = null;
        monitor.Captured -= OnCaptured;
        history.Changed -= OnHistoryChanged;
        if (tickTimer is not null)
        {
            await tickTimer.DisposeAsync().ConfigureAwait(false);
        }

        effects.Writer.TryComplete();
        await shutdown.CancelAsync().ConfigureAwait(false);
        if (effectLoop is not null)
        {
            try
            {
                await effectLoop.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                logger.Warning("Effect loop did not stop in time");
            }
        }

        shutdown.Dispose();
    }

    /// <summary>Handles a key event. Called synchronously on the keyboard hook thread; must return quickly.</summary>
    internal bool OnKey(KeyInput input)
    {
        // Effects are queued inside the lock so the effect loop sees them in exactly the order the state machine
        // produced them, even when the tick timer and the hook thread race (for example Show versus Hide).
        lock (gestureGate)
        {
            if (searching)
            {
                // The picker has the keyboard focus, and every key belongs to it (including Ctrl+V into the search box).
                // The modifiers are still tracked, so the next gesture starts from the right state.
                if (input.Key is GestureKey.Modifier or GestureKey.Shift or GestureKey.Alt or GestureKey.OtherModifier)
                {
                    gesture.Handle(input, Now());
                }

                return false;
            }

            var result = gesture.Handle(input, Now());
            UpdateNavigationCapture(gesture.IsActive);
            if (result.Effects.Any(effect => effect is BeginSearch))
            {
                searching = true;
            }

            Enqueue(result);
            return result.Suppress;
        }
    }

    /// <summary>Handles what the user does in the picker while searching. Called on the UI thread.</summary>
    /// <returns>Whether the input was used, so the search box must not handle it as well.</returns>
    internal bool OnSearchInput(PickerSearchInput input)
    {
        lock (gestureGate)
        {
            if (search is null)
            {
                return false;
            }

            var result = search.Handle(input);
            if (search.HasEnded)
            {
                search = null;
                searching = false;
            }

            Enqueue(result);
            return result.Suppress;
        }
    }

    private void OnTick()
    {
        lock (gestureGate)
        {
            if (gesture.IsActive)
            {
                Enqueue(gesture.Tick(Now()));
            }
        }
    }

    private void UpdateNavigationCapture(bool active)
    {
        if (active == navigationCaptured)
        {
            return;
        }

        navigationCaptured = active;
        try
        {
            keyboardHook.SetNavigationCaptureEnabled(active);
        }
        catch (Exception exception)
        {
            logger.Warning(exception, "Could not change navigation key capture");
        }
    }

    private void Enqueue(GestureResult result)
    {
        foreach (var effect in result.Effects)
        {
            effects.Writer.TryWrite(effect);
        }
    }

    private TimeSpan Now() => time.GetElapsedTime(startTimestamp);

    /// <summary>Captures the history and each item's actions when a gesture begins, so indexes stay stable.</summary>
    private (IReadOnlyList<GestureItem> Items, int DefaultIndex) CreateSession()
    {
        var items = history.Items;
        lock (session)
        {
            session.Clear();
            foreach (var item in items)
            {
                // Normally precomputed when the item was added; this runs on the keyboard hook thread.
                session[item.Id] = (item, availableActions.GetOrAdd(item.Id, _ => actions.GetAvailable(item)));
            }

            return (items.Select(item => new GestureItem(item.Id, session[item.Id].Actions.Count)).ToArray(),
                ClipboardHistory.GetDefaultIndex(items));
        }
    }

    private async Task ProcessEffectsAsync()
    {
        await foreach (var effect in effects.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                await ProcessAsync(effect).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failed to process {Effect}", effect.GetType().Name);
            }

            EffectProcessed?.Invoke(this, effect);
        }
    }

    private async Task ProcessAsync(GestureEffect effect)
    {
        var cancellationToken = shutdown.Token;
        switch (effect)
        {
            case ShowPicker show:
                await picker.ShowAsync(CreateViewState(show.Selection)).ConfigureAwait(false);
                break;

            case UpdatePicker update:
                await picker.UpdateAsync(CreateViewState(update.Selection)).ConfigureAwait(false);
                break;

            case HidePicker:
                await picker.HideAsync().ConfigureAwait(false);
                break;

            case BeginSearch begin:
                await BeginSearchAsync(begin).ConfigureAwait(false);
                break;

            case RestoreFocus:
                // Only the effect loop touches focusBeforeSearch.
                var restore = focusBeforeSearch;
                focusBeforeSearch = null;
                if (restore is { } window && focusTracker is not null)
                {
                    if (!await focusTracker.RestoreAsync(window, cancellationToken).ConfigureAwait(false))
                    {
                        logger.Warning("Could not give the keyboard focus back to the window it came from");
                    }
                }

                break;

            case PasteCurrentClipboard:
                logger.Debug("Pasting the current clipboard");
                await actionContext.Paste.SimulatePasteAsync(cancellationToken).ConfigureAwait(false);
                break;

            case PerformAction perform:
                await PerformAsync(perform, cancellationToken).ConfigureAwait(false);
                break;

            case DeleteItem delete:
                var removed = history.Find(delete.ItemId);
                history.Remove(delete.ItemId);
                if (removed?.IsPinned == true)
                {
                    await actionContext.PinnedItems.DeleteAsync(delete.ItemId).ConfigureAwait(false);
                }

                break;

            case GestureCancelled:
                logger.Debug("Paste gesture cancelled");
                break;
        }
    }

    private async Task BeginSearchAsync(BeginSearch begin)
    {
        // The picker has not taken the focus yet, so this is the window the user was working in.
        focusBeforeSearch = SafeCaptureFocus();
        PickerSearch created;
        lock (session)
        {
            created = new PickerSearch(begin.Selection.ItemIds
                .Where(session.ContainsKey)
                .Select(id =>
                {
                    var (item, itemActions) = session[id];
                    var content = searchTexts.GetOrAdd(id, _ => HistorySearch.CreateContentText(item));
                    return new SearchableItem(new GestureItem(id, itemActions.Count), HistorySearch.CreateSearchText(content, item));
                })
                .ToArray());
        }

        lock (gestureGate)
        {
            search = created;
        }

        logger.Debug("Searching the history");
        try
        {
            await picker.BeginSearchAsync(CreateViewState(created.Selection), OnSearchInput).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Never leave the keyboard hook passing every key through for a search that is not there.
            logger.Error(exception, "Could not begin searching");
            lock (gestureGate)
            {
                if (ReferenceEquals(search, created))
                {
                    search = null;
                    searching = false;
                }
            }

            focusBeforeSearch = null;
            await picker.HideAsync().ConfigureAwait(false);
        }
    }

    private FocusedWindow? SafeCaptureFocus()
    {
        try
        {
            return focusTracker?.Capture();
        }
        catch (Exception exception)
        {
            logger.Warning(exception, "Could not tell which window has the keyboard focus");
            return null;
        }
    }

    private async Task PerformAsync(PerformAction perform, CancellationToken cancellationToken)
    {
        (ClipboardItem Item, IReadOnlyList<IClipboardAction> Actions) entry;
        lock (session)
        {
            if (!session.TryGetValue(perform.ItemId, out entry))
            {
                logger.Warning("Selected item {Id} is no longer available", perform.ItemId);
                return;
            }
        }

        if (perform.ActionIndex < 0 || perform.ActionIndex >= entry.Actions.Count)
        {
            return;
        }

        var action = entry.Actions[perform.ActionIndex];
        logger.Information("Performing {Action} on {Kind} item", action.Id, entry.Item.Kind);
        await action.ExecuteAsync(entry.Item, actionContext, cancellationToken).ConfigureAwait(false);
    }

    private PickerViewState CreateViewState(PickerSelection selection)
    {
        if (selection.IsEmpty)
        {
            return new PickerViewState([], 0, [], 0, PickerPane.Items) { SearchQuery = selection.Query };
        }

        lock (session)
        {
            var items = selection.ItemIds.Select(id => session[id].Item).ToArray();
            var selected = session[selection.SelectedItemId];
            return new PickerViewState(
                items,
                selection.SelectedIndex,
                selected.Actions.Select(action => new PickerActionView(action.GetTitle(selected.Item), action.GetDescription(selected.Item))).ToArray(),
                selection.SelectedActionIndex,
                selection.Pane)
            {
                SearchQuery = selection.Query,
            };
        }
    }

    /// <summary>
    /// Works out each item's actions when it enters the history (content never changes afterwards), so starting a
    /// gesture inside the keyboard hook callback does no text scanning.
    /// </summary>
    private void OnHistoryChanged(object? sender, HistoryChangedEventArgs e)
    {
        switch (e.Kind)
        {
            case HistoryChangeKind.Added when e.Item is not null:
                availableActions.TryAdd(e.Item.Id, actions.GetAvailable(e.Item));
                break;
            case HistoryChangeKind.Removed when e.Item is not null:
                availableActions.TryRemove(e.Item.Id, out _);
                searchTexts.TryRemove(e.Item.Id, out _);
                break;
            case HistoryChangeKind.Reset:
                var remaining = history.Items.Select(item => item.Id).ToHashSet();
                foreach (var id in availableActions.Keys.Where(id => !remaining.Contains(id)).ToArray())
                {
                    availableActions.TryRemove(id, out _);
                }

                foreach (var id in searchTexts.Keys.Where(id => !remaining.Contains(id)).ToArray())
                {
                    searchTexts.TryRemove(id, out _);
                }

                break;
        }
    }

    private void OnCaptured(object? sender, ClipboardCapturedEventArgs e)
    {
        if (e.IsNew)
        {
            copyNotifier?.NotifyCopied(e.Item, history.Count);
        }
    }
}
