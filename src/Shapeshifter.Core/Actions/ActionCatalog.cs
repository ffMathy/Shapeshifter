using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Actions;

/// <summary>All available actions, in display order.</summary>
public sealed class ActionCatalog
{
    public ActionCatalog(IEnumerable<IClipboardAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        All = actions.OrderBy(action => action.Order).ToArray();
    }

    public static ActionCatalog CreateDefault() => new(
    [
        new PasteAction(),
        new CopyAsFileAction(),
        new PasteAsPlainTextAction(),
        new ZipFilesAction(),
        new CopyImageFromLinkAction(),
        new OpenLinksAction(),
        new TogglePinAction(),
    ]);

    public IReadOnlyList<IClipboardAction> All { get; }

    public IReadOnlyList<IClipboardAction> GetAvailable(ClipboardItem item) =>
        All.Where(action => action.CanExecute(item)).ToArray();
}
