using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Clipboard;

/// <summary>One entry of the clipboard history.</summary>
public sealed class ClipboardItem
{
    public ClipboardItem(ClipboardSnapshot snapshot, Guid? id = null, DateTimeOffset? capturedAt = null, ClipboardSource? source = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
        Source = source;
        Id = id ?? Guid.NewGuid();
        CapturedAt = capturedAt ?? DateTimeOffset.UtcNow;
        PreviewText = CreatePreviewText(snapshot);
    }

    public Guid Id { get; }

    public ClipboardSnapshot Snapshot { get; }

    /// <summary>When the content was last copied (copying the same content again moves the item up and updates this).</summary>
    public DateTimeOffset CapturedAt { get; internal set; }

    /// <summary>The application the item was copied from, when known.</summary>
    public ClipboardSource? Source { get; internal set; }

    /// <summary>Pinned items are persisted to disk, survive restarts and stay at the top of the history.</summary>
    public bool IsPinned { get; internal set; }

    public ClipboardContentKind Kind => Snapshot.Kind;

    /// <summary>A short, single-line description suitable for display.</summary>
    public string PreviewText { get; }

    public override string ToString() => $"{Kind}: {PreviewText}";

    private static string CreatePreviewText(ClipboardSnapshot snapshot)
    {
        switch (snapshot.Kind)
        {
            case ClipboardContentKind.File:
                return Path.GetFileName(snapshot.Files[0].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            case ClipboardContentKind.FileCollection:
                return $"{snapshot.Files.Count} files";
            case ClipboardContentKind.Image:
                return "Image";
            case ClipboardContentKind.Text when !string.IsNullOrEmpty(snapshot.Text):
                return TextPreview.Create(snapshot.Text);
            case ClipboardContentKind.Text:
                var rich = ClipboardFormatClassifier.FindRichText(snapshot);
                return rich is not null && ClipboardFormatClassifier.IsHtml(rich.Name)
                    ? TextPreview.Create(HtmlText.ToPlainText(HtmlText.Decode(rich.Data)))
                    : "Formatted text";
            case ClipboardContentKind.Other:
                return string.Join(", ", snapshot.RawFormats.Select(f => f.Name).Take(3));
            default:
                return string.Empty;
        }
    }
}
