using Avalonia.Media;
using Avalonia.Media.Imaging;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>One history item in the picker.</summary>
/// <remarks>
/// Each kind of content has its own layout, as in the original design: text shows two lines and the window it was
/// copied from; an image fills the row; several files show each file's icon with a count and their folder; one
/// file shows its name in large type and its folder.
/// </remarks>
public sealed class PickerItemViewModel
{
    // A page with ruled lines and a folded corner (62 × 72), drawn with even-odd filling so the lines are cut out.
    private static readonly Geometry TextGlyph = Geometry.Parse(
        "F0 M0,0 H46 L62,16 V72 H0 Z M46,4 V16 H58 Z M10,14 H36 V18 H10 Z M10,24 H52 V28 H10 Z M10,34 H52 V38 H10 Z " +
        "M10,44 H52 V48 H10 Z M10,54 H52 V58 H10 Z");

    // A framed picture of a tulip (62 × 50).
    private static readonly Geometry ImageGlyph = Geometry.Parse(
        "F0 M0,0 H62 V50 H0 Z M6,6 H56 V44 H6 Z " +
        "M23,12 L27,17 L31,10 L35,17 L39,12 V21 C39,26 35.5,29 31,29 C26.5,29 23,26 23,21 Z " +
        "M30,29 H32 V39 H30 Z M30,39 C27,33 21,31 16,32 C20,34 24,37 27,39 Z M32,39 C35,33 41,31 46,32 C42,34 38,37 35,39 Z");

    // A plain page with a folded corner (62 × 72), for single files when the system has no icon for them.
    private static readonly Geometry FileGlyph = Geometry.Parse("F0 M0,0 H46 L62,16 V72 H0 Z M46,4 V16 H58 Z");

    // A clipboard (62 × 72), for content that is neither text, an image nor files.
    private static readonly Geometry OtherGlyph = Geometry.Parse(
        "F0 M4,8 H22 C22,3 26,0 31,0 C36,0 40,3 40,8 H58 V72 H4 Z M10,14 V66 H52 V14 Z M27,8 C27,10 29,11 31,11 C33,11 35,10 35,8 C35,6 33,5 31,5 C29,5 27,6 27,8 Z");

    // A folder (16 × 12), for a folder line when the system has no icon for the folder.
    private static readonly Geometry FolderGlyph = Geometry.Parse("M0,1 C0,0.4 0.4,0 1,0 H6 L8,2 H15 C15.6,2 16,2.4 16,3 V11 C16,11.6 15.6,12 15,12 H1 C0.4,12 0,11.6 0,11 Z");

    /// <summary>How many file icons fit in a row of the history.</summary>
    public const int MaxFileGroups = 5;

    public PickerItemViewModel(ClipboardItem item, Bitmap? thumbnail, PickerVisuals? visuals = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Id = item.Id;
        Kind = item.Kind;
        IsPinned = item.IsPinned;
        Thumbnail = thumbnail;
        Title = string.IsNullOrWhiteSpace(item.PreviewText) ? "(empty)" : item.PreviewText;
        SourceIcon = visuals?.GetSourceIcon(item.Source);
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
            ClipboardContentKind.Text => TextGlyph,
            ClipboardContentKind.Image => ImageGlyph,
            ClipboardContentKind.File => FileGlyph,
            _ => OtherGlyph,
        };

        if (files.Count > 0)
        {
            var folder = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(files[0]));
            FolderName = string.IsNullOrEmpty(folder) ? null : Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) is { Length: > 0 } name ? name : folder;
            FolderIcon = folder is null ? null : visuals?.GetFileIcon(Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar, 16);
            FileCount = files.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
            FileIcon = item.Kind == ClipboardContentKind.File ? visuals?.GetFileIcon(files[0], 64) : null;
            FileGroups = files
                .GroupBy(PickerVisuals.GetIconKey)
                .Take(MaxFileGroups)
                .Select(group => new PickerFileGroupViewModel(visuals?.GetFileIcon(group.First(), 48), group.Count()))
                .ToArray();
        }

        SourceText = item.Source?.DisplayText ?? CreateFallbackSubtitle(item);
        if (IsPinned)
        {
            SourceText = $"Pinned · {SourceText}";
            FolderName = FolderName is null ? "Pinned" : $"Pinned · {FolderName}";
        }
    }

    public Guid Id { get; }

    public ClipboardContentKind Kind { get; }

    public bool IsPinned { get; }

    public PickerItemLayout Layout { get; }

    public bool IsTextLayout => Layout == PickerItemLayout.Text;

    public bool IsImageLayout => Layout == PickerItemLayout.Image;

    public bool IsFilesLayout => Layout == PickerItemLayout.Files;

    public bool IsFileLayout => Layout == PickerItemLayout.File;

    /// <summary>Two lines of text, or the file name.</summary>
    public string Title { get; }

    /// <summary>The icon shown on the left when <see cref="FileIcon"/> is not available and it is not a file list.</summary>
    public Geometry Glyph { get; }

    public bool ShowsGlyph => !IsFilesLayout && FileIcon is null;

    /// <summary>The number of copied files, shown instead of an icon on the left of a file list.</summary>
    public string FileCount { get; } = string.Empty;

    public Bitmap? Thumbnail { get; }

    /// <summary>The icon of the application the item was copied from.</summary>
    public Bitmap? SourceIcon { get; }

    public bool HasSourceIcon => SourceIcon is not null;

    /// <summary>The window the item was copied from, or a description of the content when that is unknown.</summary>
    public string SourceText { get; }

    /// <summary>The system icon of a single copied file.</summary>
    public Bitmap? FileIcon { get; }

    public bool HasFileIcon => FileIcon is not null;

    public IReadOnlyList<PickerFileGroupViewModel> FileGroups { get; } = [];

    /// <summary>The name of the folder the files were copied from.</summary>
    public string? FolderName { get; }

    public Bitmap? FolderIcon { get; }

    public bool HasFolderIcon => FolderIcon is not null;

    public bool HasNoFolderIcon => FolderIcon is null;

    public static Geometry FallbackFolderGlyph => FolderGlyph;

    private static string CreateFallbackSubtitle(ClipboardItem item)
    {
        var snapshot = item.Snapshot;
        return item.Kind switch
        {
            ClipboardContentKind.Text when ClipboardFormatClassifier.FindRichText(snapshot) is not null => "Formatted text",
            ClipboardContentKind.Text => CountLines(snapshot.Text),
            ClipboardContentKind.Image => "Image",
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

/// <summary>Copied files that share an icon, shown as that icon with the number of files on a badge.</summary>
public sealed class PickerFileGroupViewModel(Bitmap? icon, int count)
{
    private static readonly Geometry GenericFileGlyph = Geometry.Parse("F0 M0,0 H30 L40,10 V48 H0 Z M30,3 V10 H37 Z");

    public Bitmap? Icon { get; } = icon;

    public bool HasIcon => Icon is not null;

    public bool HasNoIcon => Icon is null;

    public static Geometry FallbackGlyph => GenericFileGlyph;

    public int Count { get; } = count;

    public string CountText => Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
}
