using Avalonia.Media;
using Avalonia.Media.Imaging;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>One history item in the picker.</summary>
/// <remarks>
/// Each kind of content has its own layout, ported from the original Shapeshifter (the WPF version's
/// <c>Clipboard*DataControl.xaml</c>): text shows two lines and the window it was copied from; an image is shown
/// through tilted strips with the source icon on the white area beside them; several files show each kind of file's
/// icon with a count; one file shows its icon and its name in large type.
/// </remarks>
public sealed class PickerItemViewModel
{
    // The original icons (Templarian's Modern UI icons, as used by the WPF version), drawn 50 × 60.
    private static readonly Geometry TextGlyph = Geometry.Parse(
        "F1 M 20,17L 44.25,17L 56,28.75L 56,59L 20,59L 20,17 Z M 24,21L 24,55L 52,55L 52,33L 40,33L 40,21L 24,21 Z " +
        "M 44,22.25L 44,29L 50.75,29L 44,22.25 Z M 26,23L 38,23L 38,28L 26,28L 26,23 Z M 26,30L 38,30L 38,33L 26,33L 26,30 Z " +
        "M 26,35L 50,35L 50,38L 26,38L 26,35 Z M 26,40L 50,40L 50,43L 26,43L 26,40 Z M 26,45L 50,45L 50,48L 26,48L 26,45 Z " +
        "M 26,50L 50,50L 50,53L 26,53L 26,50 Z ");

    private static readonly Geometry ImageGlyph = Geometry.Parse(
        "F1 M 17,20L 59,20L 59,56L 17,56L 17,20 Z M 20,23L 20,53L 56,53L 56,23L 20,23 Z " +
        "M 27,37.5L 34,44L 34,51C 31.6667,51 29.3333,51 28.1667,48.75C 27,46.5 27,42 27,37.5 Z " +
        "M 48.9999,37.5C 48.9999,42 48.9999,46.5 47.8333,48.75C 46.6666,51 44.3333,51 42,51L 40,51L 36,51L 36,41L 31,36L 31,30L 33,25" +
        "L 35.25,29.5001L 37.9999,25.0001L 40.75,29.5001L 43,25L 45,30L 45,36L 40,41L 40,44L 42,44L 48.9999,37.5 Z ");

    // A page with a folded corner, for a single file when the system has no icon for it.
    private static readonly Geometry FileGlyph = Geometry.Parse(
        "F1 M 20,17L 44.25,17L 56,28.75L 56,59L 20,59L 20,17 Z M 24,21L 24,55L 52,55L 52,33L 40,33L 40,21L 24,21 Z M 44,22.25L 44,29L 50.75,29L 44,22.25 Z");

    // A clipboard, for content that is neither text, an image nor files.
    private static readonly Geometry OtherGlyph = Geometry.Parse(
        "F0 M4,8 H22 C22,3 26,0 31,0 C36,0 40,3 40,8 H58 V72 H4 Z M10,14 V66 H52 V14 Z M27,8 C27,10 29,11 31,11 C33,11 35,10 35,8 C35,6 33,5 31,5 C29,5 27,6 27,8 Z");

    // A folder (16 × 12), for the folder of copied files when the system has no icon for it.
    private static readonly Geometry FolderGlyph = Geometry.Parse("M0,1 C0,0.4 0.4,0 1,0 H6 L8,2 H15 C15.6,2 16,2.4 16,3 V11 C16,11.6 15.6,12 15,12 H1 C0.4,12 0,11.6 0,11 Z");

    /// <summary>Size of the file icons in a file list, as in the original.</summary>
    public const int FileGroupIconSize = 32;

    /// <summary>How many kinds of file fit in a row of the history.</summary>
    public const int MaxFileGroups = 7;

    public PickerItemViewModel(ClipboardItem item, Bitmap? thumbnail, PickerVisuals? visuals = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Id = item.Id;
        Kind = item.Kind;
        IsPinned = item.IsPinned;
        Thumbnail = thumbnail;
        Title = string.IsNullOrWhiteSpace(item.PreviewText) ? "(empty)" : item.PreviewText;
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
            FileCount = files.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
            FileIcon = item.Kind == ClipboardContentKind.File ? visuals?.GetFileIcon(files[0], 64) : null;
            FileGroups = files
                .GroupBy(PickerVisuals.GetIconKey)
                .Take(MaxFileGroups)
                .Select(group => new PickerFileGroupViewModel(visuals?.GetFileIcon(group.First(), FileGroupIconSize), group.Count()))
                .ToArray();
        }

        // The line at the bottom shows where the item was copied from. When that is unknown, files show their folder
        // (usually the same thing: the file manager window is named after it) and text describes itself.
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
                ShowsFolderGlyph = SourceIcon is null;
            }
        }
        else
        {
            SourceText = CreateFallbackSubtitle(item);
        }

        if (IsPinned)
        {
            SourceText = string.IsNullOrEmpty(SourceText) ? "Pinned" : $"Pinned · {SourceText}";
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

    /// <summary>The icon of the application the item was copied from (or of the folder of copied files).</summary>
    public Bitmap? SourceIcon { get; }

    public bool HasSourceIcon => SourceIcon is not null;

    /// <summary>Shown instead of <see cref="SourceIcon"/> for the folder of copied files when it has no system icon.</summary>
    public bool ShowsFolderGlyph { get; }

    public static Geometry FallbackFolderGlyph => FolderGlyph;

    /// <summary>The window the item was copied from, or the folder or a description of the content when that is unknown.</summary>
    public string SourceText { get; } = string.Empty;

    public bool HasSourceText => SourceText.Length > 0;

    /// <summary>The system icon of a single copied file.</summary>
    public Bitmap? FileIcon { get; }

    public bool HasFileIcon => FileIcon is not null;

    public IReadOnlyList<PickerFileGroupViewModel> FileGroups { get; } = [];

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

/// <summary>Copied files that share an icon, shown as that icon with the number of files on a badge.</summary>
public sealed class PickerFileGroupViewModel(Bitmap? icon, int count)
{
    private static readonly Geometry GenericFileGlyph = Geometry.Parse("F0 M0,0 H22 L28,6 V32 H0 Z M22,2 V6 H26 Z");

    public Bitmap? Icon { get; } = icon;

    public bool HasIcon => Icon is not null;

    public bool HasNoIcon => Icon is null;

    public static Geometry FallbackGlyph => GenericFileGlyph;

    public int Count { get; } = count;

    public string CountText => Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
}
