using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Actions;

/// <summary>Pins an item (keeps it at the top and on disk) or unpins it.</summary>
public sealed class TogglePinAction : IClipboardAction
{
    public string Id => "toggle-pin";

    public int Order => 255;

    public string GetTitle(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.IsPinned ? "Unpin from clipboard" : "Pin to clipboard";
    }

    public string GetDescription(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.IsPinned
            ? "Unpins this item so that it moves down as you copy, and is no longer saved to your harddrive."
            : "Pins this item so that it always stays in the top, and gets saved to your harddrive so that it is there the next time you open Shapeshifter.";
    }

    public bool CanExecute(ClipboardItem item) => true;

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        if (item.IsPinned)
        {
            context.History.SetPinned(item.Id, pinned: false);
            await context.PinnedItems.DeleteAsync(item.Id).ConfigureAwait(false);
        }
        else
        {
            await context.PinnedItems.SaveAsync(item, cancellationToken).ConfigureAwait(false);
            context.History.SetPinned(item.Id, pinned: true);
        }
    }
}
