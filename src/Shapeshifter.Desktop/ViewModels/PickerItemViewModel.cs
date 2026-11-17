using System.Buffers.Binary;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Text;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>One history item in the picker: a tile on the left, a title and the place it came from.</summary>
/// <remarks>
/// The tile shows what kind of item it is: a thumbnail for an image, the system icon of a single file, up to four
/// file icons for several files, and otherwise a line icon. The title is the text itself, the file name, "Image ·
/// 1920 × 1080" or "4 items · …"; the line below names the window it was copied from, or the folder of copied files.
/// On the right, every row shows when it was copied and, when it is pinned, a pin.
/// </remarks>
public sealed partial class PickerItemViewModel : ObservableObject
{
    /// <summary>Size of the file icons that are requested from the operating system.</summary>
    public const int FileIconSize = 32;

    /// <summary>How many file icons fit on the tile of several files.</summary>
    public const int MaxFilePreviews = 4;

    public PickerItemViewModel(ClipboardItem item, Bitmap? thumbnail, PickerVisuals? visuals = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Id = item.Id;
        Kind = item.Kind;
        IsPinned = item.IsPinned;
        this.item = item;
        copiedAgo = RelativeTime.Describe(CapturedAt, DateTimeOffset.Now);
        Thumbnail = thumbnail;
        var files = item.Snapshot.Files;

        Layout = item.Kind switch
        {
            ClipboardContentKind.Image when thumbnail is not null => PickerItemLayout.Image,
            ClipboardContentKind.FileCollection => PickerItemLayout.Files,
            ClipboardContentKind.File => PickerItemLayout.File,
            _ => PickerItemLayout.Text,
        };

        Glyph = item.Kind switch
        {
            ClipboardContentKind.Text => Glyphs.Text,
            ClipboardContentKind.Image => Glyphs.Image,
            ClipboardContentKind.File => Glyphs.File,
            ClipboardContentKind.FileCollection => Glyphs.Folder,
            _ => Glyphs.Other,
        };

        Title = item.Kind switch
        {
            ClipboardContentKind.Image => DescribeImage(item.Snapshot.Image),
            ClipboardContentKind.FileCollection => DescribeFiles(files),
            ClipboardContentKind.File => Path.GetFileName(Path.TrimEndingDirectorySeparator(files[0])),
            _ => string.IsNullOrWhiteSpace(item.PreviewText) ? "(empty)" : item.PreviewText,
        };

        // A copied colour code ("#2c5497", "rgb(44, 84, 151)") shows the colour itself on its tile (issue #299).
        if (item.Kind == ClipboardContentKind.Text && ColorParser.TryParse(item.Snapshot.Text, out var color))
        {
            ColorSwatch = new ImmutableSolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        if (files.Count > 0)
        {
            FileIcon = item.Kind == ClipboardContentKind.File ? visuals?.GetFileIcon(files[0], FileIconSize * 2) : null;
            FilePreviews = files
                .GroupBy(PickerVisuals.GetIconKey)
                .Take(MaxFilePreviews)
                .Select(group => new PickerFilePreviewViewModel(visuals?.GetFileIcon(group.First(), FileIconSize)))
                .ToArray();
        }

        // The line below the title shows where the item was copied from. When that is unknown, files show their
        // folder (usually the same thing: the file manager window is named after it) and text describes itself.
        if (item.Source is { } source)
        {
            SourceIcon = visuals?.GetSourceIcon(source);
            SourceText = source.DisplayText ?? string.Empty;
        }
        else if (files.Count > 0)
        {
            var folder = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(files[0]));
            if (!string.IsNullOrEmpty(folder))
            {
                SourceText = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) is { Length: > 0 } name ? name : folder;
                SourceIcon = visuals?.GetFileIcon(Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar, 16);
                SourceGlyph = Glyphs.Folder;
            }
        }
        else
        {
            SourceText = CreateFallbackSubtitle(item);
        }

        PreviewText = CreatePreviewText(item);
        PreviewCaption = CreatePreviewCaption(item, Title);
    }

    public Guid Id { get; }

    public ClipboardContentKind Kind { get; }

    public bool IsPinned { get; }

    private readonly ClipboardItem item;

    /// <summary>When the item was last copied (copying it again updates this, so it is read from the item).</summary>
    public DateTimeOffset CapturedAt => item.CapturedAt;

    /// <summary><see cref="CapturedAt"/> as "Just now", "5 min ago" and so on; see <see cref="RefreshCopiedAgo"/>.</summary>
    [ObservableProperty]
    private string copiedAgo;

    /// <summary>Updates <see cref="CopiedAgo"/>, since the picker reuses its rows each time it is shown.</summary>
    public void RefreshCopiedAgo(DateTimeOffset now) => CopiedAgo = RelativeTime.Describe(CapturedAt, now);

    public PickerItemLayout Layout { get; }

    public bool IsTextLayout => Layout == PickerItemLayout.Text;

    public bool IsImageLayout => Layout == PickerItemLayout.Image;

    public bool IsFilesLayout => Layout == PickerItemLayout.Files;

    public bool IsFileLayout => Layout == PickerItemLayout.File;

    /// <summary>One or two lines of text, the file name, or a description of an image or of several files.</summary>
    public string Title { get; }

    /// <summary>The line icon on the tile when there is no thumbnail or file icon.</summary>
    public Geometry Glyph { get; }

    public bool ShowsGlyph => Thumbnail is null && FileIcon is null && FilePreviews.Count == 0 && ColorSwatch is null;

    /// <summary>The colour of a copied colour code, filling the tile.</summary>
    public IBrush? ColorSwatch { get; }

    public bool HasColorSwatch => ColorSwatch is not null;

    /// <summary>A copied image, filling the tile.</summary>
    public Bitmap? Thumbnail { get; }

    public bool HasThumbnail => Thumbnail is not null;

    /// <summary>The system icon of a single copied file.</summary>
    public Bitmap? FileIcon { get; }

    public bool HasFileIcon => FileIcon is not null;

    /// <summary>The icons of up to four kinds of copied files, shown two by two on the tile.</summary>
    public IReadOnlyList<PickerFilePreviewViewModel> FilePreviews { get; } = [];

    public bool HasFilePreviews => FilePreviews.Count > 0;

    /// <summary>The icon of the application the item was copied from (or of the folder of copied files).</summary>
    public Bitmap? SourceIcon { get; }

    public bool HasSourceIcon => SourceIcon is not null;

    /// <summary>Shown when there is no <see cref="SourceIcon"/>: a globe for windows, a folder for the folder of files.</summary>
    public Geometry SourceGlyph { get; } = Glyphs.Source;

    public bool ShowsSourceGlyph => SourceIcon is null && SourceText.Length > 0;

    /// <summary>The window the item was copied from, or the folder or a description of the content when that is unknown.</summary>
    public string SourceText { get; } = string.Empty;

    public bool HasSourceText => SourceText.Length > 0;

    /// <summary>
    /// What the preview pane above the actions shows of a text or of files: much more of the text than the row's one
    /// or two lines, or every file name. Empty for images, which show <see cref="PreviewImage"/> instead.
    /// </summary>
    public string PreviewText { get; }

    public bool HasPreviewText => PreviewText.Length > 0;

    /// <summary>The line under the preview: "3 lines · 120 characters", "Image · 1920 × 1080", "4 files and folders".</summary>
    public string PreviewCaption { get; }

    /// <summary>
    /// The copied image decoded larger than <see cref="Thumbnail"/>, for the preview pane. Only the selected item has
    /// one (the presenter decodes it when the item is selected and drops it when another is).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewImage))]
    [NotifyPropertyChangedFor(nameof(ShowsPreviewImage))]
    private Bitmap? largeImage;

    /// <summary>The image in the preview pane: the large one once it is decoded, the thumbnail until then.</summary>
    public Bitmap? PreviewImage => LargeImage ?? Thumbnail;

    public bool ShowsPreviewImage => PreviewImage is not null && Layout == PickerItemLayout.Image;

    /// <summary>A large line icon when there is nothing else to preview (an image that could not be decoded, other data).</summary>
    public bool ShowsPreviewGlyph => !HasPreviewText && !ShowsPreviewImage && ColorSwatch is null;

    /// <summary>How much of a text the preview shows; the pane only has room for a screenful anyway.</summary>
    internal const int MaxPreviewLength = 2000;

    /// <summary>How many file names the preview lists.</summary>
    internal const int MaxPreviewFiles = 30;

    internal static string CreatePreviewText(ClipboardItem item)
    {
        var files = item.Snapshot.Files;
        switch (item.Kind)
        {
            case ClipboardContentKind.File:
                return files[0];
            case ClipboardContentKind.FileCollection:
                var names = files.Take(MaxPreviewFiles).Select(file => Path.GetFileName(Path.TrimEndingDirectorySeparator(file)));
                var more = files.Count > MaxPreviewFiles
                    ? string.Create(CultureInfo.CurrentCulture, $"\n… and {files.Count - MaxPreviewFiles} more")
                    : string.Empty;
                return string.Join('\n', names) + more;
            case ClipboardContentKind.Text when ColorParser.TryParse(item.Snapshot.Text, out _):
                // A colour code shows its colour, filling the pane.
                return string.Empty;
            case ClipboardContentKind.Text:
                var text = item.Snapshot.Text ?? item.PreviewText ?? string.Empty;
                if (text.Length > MaxPreviewLength)
                {
                    text = text[..MaxPreviewLength] + "…";
                }

                // Line endings of every platform show as one line break, and tabs as a few spaces (a tab is drawn as
                // nothing, so indented code would lose its indentation).
                return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\t", "    ", StringComparison.Ordinal);
            default:
                return string.Empty;
        }
    }

    internal static string CreatePreviewCaption(ClipboardItem item, string title)
    {
        var snapshot = item.Snapshot;
        switch (item.Kind)
        {
            case ClipboardContentKind.Image:
                return title;
            case ClipboardContentKind.File:
                return "File";
            case ClipboardContentKind.FileCollection:
                return string.Create(CultureInfo.CurrentCulture, $"{snapshot.Files.Count} files and folders");
            case ClipboardContentKind.Text when ColorParser.TryParse(snapshot.Text, out _):
                return "Colour · " + snapshot.Text!.Trim();
            case ClipboardContentKind.Text:
                var text = snapshot.Text ?? string.Empty;
                var lines = text.Length == 0 ? 0 : text.AsSpan().Count('\n') + 1;
                var size = lines <= 1
                    ? string.Create(CultureInfo.CurrentCulture, $"{text.Length} characters")
                    : string.Create(CultureInfo.CurrentCulture, $"{lines} lines · {text.Length} characters");
                return ClipboardFormatClassifier.FindRichText(snapshot) is not null ? "Formatted text · " + size : size;
            default:
                return "Other data";
        }
    }

    /// <summary>The copied PNG, for an image.</summary>
    public byte[]? Image => item.Snapshot.Image;

    /// <summary>"Image · 1920 × 1080" when the size can be read from the PNG header, otherwise "Image".</summary>
    internal static string DescribeImage(byte[]? png) => ReadPngSize(png) is { } size
        ? string.Create(CultureInfo.CurrentCulture, $"Image · {size.Width} × {size.Height}")
        : "Image";

    /// <summary>The size of a PNG, read from its header, or null when it is not a PNG.</summary>
    internal static (uint Width, uint Height)? ReadPngSize(byte[]? png)
    {
        // A PNG starts with an 8-byte signature followed by the IHDR chunk: length, type, width, height (big-endian).
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (png is { Length: >= 24 } && png.AsSpan(0, 8).SequenceEqual(signature))
        {
            return (BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)), BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));
        }

        return null;
    }

    /// <summary>"4 items · Projects, Notes.txt, Holiday.mp4, Sunset.png".</summary>
    internal static string DescribeFiles(IReadOnlyList<string> files)
    {
        var names = string.Join(", ", files.Take(6).Select(file => Path.GetFileName(Path.TrimEndingDirectorySeparator(file))));
        return string.Create(CultureInfo.CurrentCulture, $"{files.Count} items · {names}");
    }

    private static string CreateFallbackSubtitle(ClipboardItem item)
    {
        var snapshot = item.Snapshot;
        return item.Kind switch
        {
            ClipboardContentKind.Text when ClipboardFormatClassifier.FindRichText(snapshot) is not null => "Formatted text",
            ClipboardContentKind.Text => CountLines(snapshot.Text),
            ClipboardContentKind.Image => string.Empty,
            _ => "Other data",
        };
    }

    private static string CountLines(string? text)
    {
        var lines = string.IsNullOrEmpty(text) ? 0 : text.AsSpan().Count('\n') + 1;
        return lines <= 1 ? $"{text?.Length ?? 0} characters" : $"{lines} lines";
    }
}

/// <summary>How a history item is laid out.</summary>
public enum PickerItemLayout
{
    Text,
    Image,
    Files,
    File,
}

/// <summary>The icon of one kind of copied file on the tile of several files.</summary>
public sealed class PickerFilePreviewViewModel(Bitmap? icon)
{
    public Bitmap? Icon { get; } = icon;

    public bool HasIcon => Icon is not null;

    public bool HasNoIcon => Icon is null;

    public static Geometry FallbackGlyph => Glyphs.File;
}

/// <summary>One action of the selected item, with its line icon.</summary>
public sealed class PickerActionViewModel(Shapeshifter.Core.Engine.PickerActionView view, bool isDefault)
{
    public Shapeshifter.Core.Engine.PickerActionView View { get; } = view;

    public string Title => View.Title;

    public string Description => View.Description;

    public Geometry Glyph { get; } = Glyphs.ForAction(view?.Title ?? string.Empty);

    /// <summary>The action that runs when the modifier is released without choosing another (the first).</summary>
    public bool IsDefault { get; } = isDefault;
}
