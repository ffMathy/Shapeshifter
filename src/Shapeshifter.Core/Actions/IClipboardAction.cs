using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Persistence;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Actions;

/// <summary>Something the user can do with a history item from the picker.</summary>
public interface IClipboardAction
{
    /// <summary>Stable identifier.</summary>
    string Id { get; }

    /// <summary>Title shown in the actions pane. May depend on the item (e.g. "Pin" vs "Unpin").</summary>
    string GetTitle(ClipboardItem item);

    /// <summary>One sentence shown below the title in the actions pane, describing what the action does.</summary>
    string GetDescription(ClipboardItem item);

    /// <summary>Lower values are listed first. The first action is the default.</summary>
    int Order { get; }

    bool CanExecute(ClipboardItem item);

    Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default);
}

/// <summary>Services an action may use.</summary>
public sealed class ActionContext
{
    public required ClipboardMonitor Clipboard { get; init; }

    public required IPasteSimulator Paste { get; init; }

    public required ClipboardHistory History { get; init; }

    public required PinnedItemStore PinnedItems { get; init; }

    public required ILauncher Launcher { get; init; }

    public required AppPaths Paths { get; init; }

    public required HttpClient Http { get; init; }

    public ILogger Logger { get; init; } = Log.Logger;

    /// <summary>
    /// Puts <paramref name="snapshot"/> on the clipboard and pastes it into the focused application.
    /// </summary>
    public async Task WriteAndPasteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken)
    {
        await Clipboard.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);

        // Give the clipboard owner a moment to settle before the target application asks for the data.
        await Task.Delay(PasteSettleDelay, cancellationToken).ConfigureAwait(false);
        await Paste.SimulatePasteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts newly created content on the clipboard and makes it the newest history item, so that a plain
    /// paste afterwards inserts it.
    /// </summary>
    public async Task CopyNewContentAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken)
    {
        await Clipboard.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);
        History.Add(snapshot);
    }

    public static TimeSpan PasteSettleDelay { get; set; } = TimeSpan.FromMilliseconds(40);
}
