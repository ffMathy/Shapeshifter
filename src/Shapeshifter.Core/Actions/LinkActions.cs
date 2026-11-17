using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Previews;
using Shapeshifter.Core.Text;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Actions;

/// <summary>Opens every link in the copied text in the default browser.</summary>
public sealed class OpenLinksAction : IClipboardAction
{
    public const int MaxLinks = 10;

    public string Id => "open-links";

    public int Order => 200;

    public string GetTitle(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return LinkParser.ExtractLinks(item.Snapshot.Text).Count == 1 ? Strings.ActionOpenLink : Strings.ActionOpenLinks;
    }

    public string GetDescription(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return LinkParser.ExtractLinks(item.Snapshot.Text).Count == 1
            ? Strings.ActionOpenLinkDescription
            : Localizer.Format(Strings.ActionOpenLinksDescription, MaxLinks);
    }

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Text && LinkParser.ExtractLinks(item.Snapshot.Text).Count > 0;
    }

    public Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        foreach (var link in LinkParser.ExtractLinks(item.Snapshot.Text).Take(MaxLinks))
        {
            context.Launcher.OpenUrl(link);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Downloads the image a copied link points to and copies the image itself.</summary>
public sealed class CopyImageFromLinkAction : IClipboardAction
{
    public const long MaxImageBytes = 25 * 1024 * 1024;

    public string Id => "copy-image-from-link";

    public int Order => 100;

    public string GetTitle(ClipboardItem item) => Strings.ActionCopyImageFromLink;

    public string GetDescription(ClipboardItem item) => Strings.ActionCopyImageFromLinkDescription;

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Text && LinkParser.ExtractLinks(item.Snapshot.Text).Any(LinkParser.IsImageLink);
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        var link = LinkParser.ExtractLinks(item.Snapshot.Text).First(LinkParser.IsImageLink);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        var bytes = await ImageDownload.DownloadAsync(context.Http, link, MaxImageBytes, timeout.Token).ConfigureAwait(false);
        await context.CopyNewContentAsync(new ClipboardSnapshot { Image = bytes }, cancellationToken).ConfigureAwait(false);
    }
}
