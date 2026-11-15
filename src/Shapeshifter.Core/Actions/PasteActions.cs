using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Actions;

/// <summary>Restores the item with all of its formats and pastes it.</summary>
public sealed class PasteAction : IClipboardAction
{
    public string Id => "paste";

    public int Order => 0;

    public string GetTitle(ClipboardItem item) => "Paste";

    public bool CanExecute(ClipboardItem item) => true;

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        await context.WriteAndPasteAsync(item.Snapshot, cancellationToken).ConfigureAwait(false);
        context.History.MoveToTop(item.Id);
    }
}

/// <summary>Pastes only the plain text of formatted (HTML or RTF) text.</summary>
public sealed class PasteAsPlainTextAction : IClipboardAction
{
    public string Id => "paste-plain-text";

    public int Order => 10;

    public string GetTitle(ClipboardItem item) => "Paste as plain text";

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Text && ClipboardFormatClassifier.FindRichText(item.Snapshot) is not null;
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        var text = item.Snapshot.Text;
        if (string.IsNullOrEmpty(text))
        {
            var rich = ClipboardFormatClassifier.FindRichText(item.Snapshot);
            text = rich is not null && ClipboardFormatClassifier.IsHtml(rich.Name)
                ? HtmlText.ToPlainText(HtmlText.Decode(rich.Data))
                : item.PreviewText;
        }

        await context.WriteAndPasteAsync(new ClipboardSnapshot { Text = text }, cancellationToken).ConfigureAwait(false);
    }
}
