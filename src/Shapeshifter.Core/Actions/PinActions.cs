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
        return item.IsPinned ? "Unpin" : "Pin to top";
    }

    public string GetDescription(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.IsPinned
            ? "Lets the item move down the history again and stops keeping it after a restart."
            : "Keeps the item at the top of the history and saves it, so it is still there after a restart.";
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
