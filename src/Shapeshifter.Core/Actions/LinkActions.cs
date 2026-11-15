using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Text;

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
        return LinkParser.ExtractLinks(item.Snapshot.Text).Count == 1 ? "Open link" : "Open links";
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

    public string GetTitle(ClipboardItem item) => "Copy image from link";

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

        using var response = await context.Http.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxImageBytes)
        {
            throw new InvalidOperationException($"The image at {link} is too large to copy.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{link} did not return an image ({mediaType}).");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
        await context.CopyNewContentAsync(new ClipboardSnapshot { Image = bytes }, cancellationToken).ConfigureAwait(false);
    }
}
