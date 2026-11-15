using System.Threading.Channels;
using Serilog;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Engine;

/// <summary>What the picker displays.</summary>
public sealed record PickerViewState(
    IReadOnlyList<ClipboardItem> Items,
    int SelectedIndex,
    IReadOnlyList<string> Actions,
    int SelectedActionIndex,
    PickerPane Pane);

/// <summary>The picker UI. Implementations marshal to their UI thread and must not take keyboard focus.</summary>
public interface IPickerPresenter
{
    Task ShowAsync(PickerViewState state);

    Task UpdateAsync(PickerViewState state);

    Task HideAsync();
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
    private readonly ILogger logger;
    private readonly TimeProvider time;
    private readonly PasteGestureStateMachine gesture;
    private readonly Lock gestureGate = new();
    private readonly Channel<GestureEffect> effects = Channel.CreateUnbounded<GestureEffect>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Dictionary<Guid, (ClipboardItem Item, IReadOnlyList<IClipboardAction> Actions)> session = [];
    private readonly long startTimestamp;
    private readonly CancellationTokenSource shutdown = new();
    private ITimer? tickTimer;
    private Task? effectLoop;
    private bool navigationCaptured;
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
        TimeProvider? time = null)
    {
        this.history = history;
        this.monitor = monitor;
        this.keyboardHook = keyboardHook;
        this.actions = actions;
        this.actionContext = actionContext;
        this.picker = picker;
        this.copyNotifier = copyNotifier;
        this.logger = logger ?? Log.Logger;
        this.time = time ?? TimeProvider.System;
        startTimestamp = this.time.GetTimestamp();
        gesture = new PasteGestureStateMachine(CreateSession);
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
        GestureResult result;
        bool active;
        lock (gestureGate)
        {
            result = gesture.Handle(input, Now());
            active = gesture.IsActive;
        }

        UpdateNavigationCapture(active);
        Enqueue(result);
        return result.Suppress;
    }

    private void OnTick()
    {
        GestureResult result;
        lock (gestureGate)
        {
            if (!gesture.IsActive)
            {
                return;
            }

            result = gesture.Tick(Now());
        }

        Enqueue(result);
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
                session[item.Id] = (item, actions.GetAvailable(item));
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
        lock (session)
        {
            var items = selection.ItemIds.Select(id => session[id].Item).ToArray();
            var selected = session[selection.SelectedItemId];
            return new PickerViewState(
                items,
                selection.SelectedIndex,
                selected.Actions.Select(action => action.GetTitle(selected.Item)).ToArray(),
                selection.SelectedActionIndex,
                selection.Pane);
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
