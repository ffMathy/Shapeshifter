using Avalonia.Media;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Line icons on a 20 × 20 grid, drawn as strokes (the looks choose the colour and the stroke width), in the spirit of
/// Fluent System Icons, SF Symbols and the GNOME symbolic icons.
/// </summary>
public static class Glyphs
{
    public static Geometry Text { get; } = Geometry.Parse("M5,2.5 H11.5 L15.5,6.5 V17.5 H5 Z M11.5,2.5 V6.5 H15.5 M7.5,9.5 H13 M7.5,12 H13 M7.5,14.5 H11");

    public static Geometry Image { get; } = Geometry.Parse("M3,4 H17 V16 H3 Z M3,13.5 L7.5,9.5 L11,12.5 L13.5,10.5 L17,13.5 M12.5,7.5 A1,1 0 1 1 12.49,7.5");

    public static Geometry File { get; } = Geometry.Parse("M5,2.5 H11.5 L15.5,6.5 V17.5 H5 Z M11.5,2.5 V6.5 H15.5");

    public static Geometry Folder { get; } = Geometry.Parse("M2.5,5 C2.5,4.2 3.1,3.5 4,3.5 H7.5 L9.5,5.5 H16 C16.8,5.5 17.5,6.2 17.5,7 V15 C17.5,15.8 16.8,16.5 16,16.5 H4 C3.1,16.5 2.5,15.8 2.5,15 Z");

    public static Geometry Clipboard { get; } = Geometry.Parse("M7.5,3.5 H5.5 C4.9,3.5 4.5,3.9 4.5,4.5 V17 C4.5,17.6 4.9,18 5.5,18 H14.5 C15.1,18 15.5,17.6 15.5,17 V4.5 C15.5,3.9 15.1,3.5 14.5,3.5 H12.5 M7.5,2.5 H12.5 V5 H7.5 Z");

    public static Geometry PasteAsFile { get; } = Geometry.Parse("M5,2.5 H11.5 L15.5,6.5 V17.5 H5 Z M11.5,2.5 V6.5 H15.5 M10.25,9 V15 M7.75,12.5 L10.25,15 L12.75,12.5");

    public static Geometry PlainText { get; } = Geometry.Parse("M3.5,5 V3.5 H12.5 V5 M8,3.5 V16.5 M6,16.5 H10 M12.5,11 H17 M12.5,14 H17 M12.5,17 H15.5");

    public static Geometry Pin { get; } = Geometry.Parse("M12.5,2.5 L17.5,7.5 L14.5,9 L11.5,12 L11,15.5 L4.5,9 L8,8.5 L11,5.5 Z M7.75,12.25 L3,17");

    public static Geometry Unpin { get; } = Geometry.Parse("M12.5,2.5 L17.5,7.5 L14.5,9 L11.5,12 L11,15.5 L4.5,9 L8,8.5 L11,5.5 Z M7.75,12.25 L3,17 M3,3 L17,17");

    public static Geometry Archive { get; } = Geometry.Parse("M3,4 H17 V7.5 H3 Z M4,7.5 V16.5 H16 V7.5 M8.5,10.5 H11.5");

    public static Geometry Link { get; } = Geometry.Parse("M8.5,11.5 L11.5,8.5 M9.5,5.5 L11,4 C12.4,2.6 14.6,2.6 16,4 C17.4,5.4 17.4,7.6 16,9 L14.5,10.5 M10.5,14.5 L9,16 C7.6,17.4 5.4,17.4 4,16 C2.6,14.6 2.6,12.4 4,11 L5.5,9.5");

    public static Geometry Upload { get; } = Geometry.Parse("M6,15.5 H5.5 C3.6,15.5 2.5,14.2 2.5,12.5 C2.5,10.9 3.7,9.6 5.3,9.5 C5.7,6.9 7.6,5 10,5 C12.4,5 14.3,6.9 14.7,9.5 C16.3,9.6 17.5,10.9 17.5,12.5 C17.5,14.2 16.4,15.5 14.5,15.5 H14 M10,17.5 V10.5 M7.75,12.75 L10,10.5 L12.25,12.75");

    public static Geometry Other { get; } = Clipboard;

    public static Geometry Search { get; } = Geometry.Parse("M8.5,3 A5.5,5.5 0 1 1 8.49,3 M12.5,12.5 L17,17");

    public static Geometry Clock { get; } = Geometry.Parse("M10,2.5 A7.5,7.5 0 1 1 9.99,2.5 M10,6 V10 L12.75,11.75");

    public static Geometry Stack { get; } = Geometry.Parse("M4,7.5 H16 V17 H4 Z M5.5,5 H14.5 M7,2.5 H13");

    public static Geometry Bell { get; } = Geometry.Parse("M5,13.5 V9 C5,6.2 7.2,4 10,4 C12.8,4 15,6.2 15,9 V13.5 L16.5,15 H3.5 Z M8.25,17 C8.6,17.8 9.25,18 10,18 C10.75,18 11.4,17.8 11.75,17 M10,2.5 V4");

    public static Geometry Lightbulb { get; } = Geometry.Parse("M7.5,14 C7.5,12.25 5,11 5,7.75 C5,5 7.2,2.75 10,2.75 C12.8,2.75 15,5 15,7.75 C15,11 12.5,12.25 12.5,14 Z M7.75,16.25 H12.25 M8.75,18 H11.25");

    public static Geometry Power { get; } = Geometry.Parse("M10,2.5 V9.5 M6,5.25 C4.4,6.5 3.5,8.2 3.5,10.25 C3.5,13.8 6.4,16.75 10,16.75 C13.6,16.75 16.5,13.8 16.5,10.25 C16.5,8.2 15.6,6.5 14,5.25");

    public static Geometry Update { get; } = Geometry.Parse("M16.5,10 A6.5,6.5 0 1 1 14.6,5.4 M15,2.25 V5.75 H11.5 M10,6.5 V11.5 M7.75,9.5 L10,11.75 L12.25,9.5");

    public static Geometry Trash { get; } = Geometry.Parse("M3.5,5.5 H16.5 M8,5.5 V3.5 H12 V5.5 M5,5.5 L6,17 H14 L15,5.5 M8.5,8.5 V14 M11.5,8.5 V14");

    public static Geometry Source { get; } = Geometry.Parse("M10,2.5 A7.5,7.5 0 1 1 9.99,2.5 M2.5,10 H17.5 M10,2.5 C7.5,5 7.5,15 10,17.5 C12.5,15 12.5,5 10,2.5");

    /// <summary>
    /// The icon of an action, recognised by its <see cref="Shapeshifter.Core.Actions.IClipboardAction.Id"/> (and, for
    /// the pin action, by whether it pins or unpins); a plain clipboard for actions that are not known.
    /// </summary>
    public static Geometry ForAction(string id, bool unpins = false)
    {
        ArgumentNullException.ThrowIfNull(id);
        return id switch
        {
            "toggle-pin" => unpins ? Unpin : Pin,
            "paste-plain-text" => PlainText,
            "copy-as-zip" => Archive,
            "upload-image" => Upload,
            "paste-as-file" => PasteAsFile,
            "copy-image-from-link" => Image,
            "open-links" => Link,
            _ => Clipboard,
        };
    }
}
