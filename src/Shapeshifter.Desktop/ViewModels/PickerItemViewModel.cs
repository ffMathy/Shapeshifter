using Avalonia.Media;
using Avalonia.Media.Imaging;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>One history item in the picker.</summary>
public sealed class PickerItemViewModel
{
    private static readonly Geometry TextIcon = Geometry.Parse("M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M8,12V14H16V12H8M8,16V18H13V16H8Z");
    private static readonly Geometry ImageIcon = Geometry.Parse("M19,19H5V5H19M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M13.96,12.29L11.21,15.83L9.25,13.47L6.5,17H17.5L13.96,12.29Z");
    private static readonly Geometry FileIcon = Geometry.Parse("M13,9V3.5L18.5,9M6,2C4.89,2 4,2.89 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2H6Z");
    private static readonly Geometry FilesIcon = Geometry.Parse("M15,7H20.5L15,1.5V7M8,0H16L22,6V18A2,2 0 0,1 20,20H8C6.89,20 6,19.1 6,18V2A2,2 0 0,1 8,0M4,4V22H20V24H4A2,2 0 0,1 2,22V4H4Z");
    private static readonly Geometry OtherIcon = Geometry.Parse("M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3Z");

    public PickerItemViewModel(ClipboardItem item, Bitmap? thumbnail)
    {
        ArgumentNullException.ThrowIfNull(item);
        Id = item.Id;
        Kind = item.Kind;
        Title = string.IsNullOrWhiteSpace(item.PreviewText) ? "(empty)" : item.PreviewText;
        Subtitle = CreateSubtitle(item);
        Thumbnail = thumbnail;
        IsPinned = item.IsPinned;
        Icon = item.Kind switch
        {
            ClipboardContentKind.Text => TextIcon,
            ClipboardContentKind.Image => ImageIcon,
            ClipboardContentKind.File => FileIcon,
            ClipboardContentKind.FileCollection => FilesIcon,
            _ => OtherIcon,
        };
    }

    public Guid Id { get; }

    public ClipboardContentKind Kind { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public Bitmap? Thumbnail { get; }

    public bool HasThumbnail => Thumbnail is not null;

    public bool HasNoThumbnail => Thumbnail is null;

    public bool IsPinned { get; }

    public Geometry Icon { get; }

    private static string CreateSubtitle(ClipboardItem item)
    {
        var snapshot = item.Snapshot;
        var age = FormatAge(DateTimeOffset.UtcNow - item.CapturedAt);
        return item.Kind switch
        {
            ClipboardContentKind.Text when ClipboardFormatClassifier.FindRichText(snapshot) is not null => $"Formatted text · {age}",
            ClipboardContentKind.Text => $"{CountLines(snapshot.Text)} · {age}",
            ClipboardContentKind.Image => $"Image · {age}",
            ClipboardContentKind.File => $"{snapshot.Files[0]} · {age}",
            ClipboardContentKind.FileCollection => $"{string.Join(", ", snapshot.Files.Take(3).Select(Path.GetFileName))}{(snapshot.Files.Count > 3 ? ", …" : string.Empty)} · {age}",
            _ => $"Other data · {age}",
        };
    }

    private static string CountLines(string? text)
    {
        var lines = string.IsNullOrEmpty(text) ? 0 : text.AsSpan().Count('\n') + 1;
        return lines == 1 ? $"{text!.Length} characters" : $"{lines} lines";
    }

    private static string FormatAge(TimeSpan age) => age.TotalSeconds switch
    {
        < 60 => "just now",
        < 3600 => $"{(int)age.TotalMinutes} min ago",
        < 86400 => $"{(int)age.TotalHours} h ago",
        _ => $"{(int)age.TotalDays} d ago",
    };
}
