namespace Shapeshifter.Core.Platform;

/// <summary>Where a clipboard item was copied from, as shown under the item in the picker.</summary>
/// <param name="ApplicationName">The application's display name, e.g. "Google Chrome".</param>
/// <param name="WindowTitle">The title of the window the copy came from, when the platform exposes it.</param>
/// <param name="IconPng">The application's icon as a PNG image (ideally 32×32 pixels or larger), if available.</param>
public sealed record ClipboardSource(string? ApplicationName, string? WindowTitle, byte[]? IconPng)
{
    /// <summary>The text shown for the source: the window title, else the application name.</summary>
    public string? DisplayText => !string.IsNullOrWhiteSpace(WindowTitle) ? WindowTitle : ApplicationName;
}

/// <summary>Finds out which application the user is copying from.</summary>
/// <remarks>
/// Called from a thread-pool thread right after the clipboard changed. Implementations must be thread-safe, fast
/// (well under 50 ms) and must not throw; they return <see langword="null"/> when they cannot tell.
/// </remarks>
public interface IClipboardSourceLocator
{
    ClipboardSource? GetCurrentSource();
}

/// <summary>Provides the operating system's icon for a file or folder, as shown in the file manager.</summary>
/// <remarks>Called on the UI thread; implementations must not throw and should be fast.</remarks>
public interface IFileIconProvider
{
    /// <summary>
    /// Returns the icon for <paramref name="path"/> as a PNG image of roughly <paramref name="size"/> pixels square,
    /// or <see langword="null"/> when there is none. The file need not exist: the icon may be derived from the
    /// extension alone (a path ending in a directory separator, or an existing directory, is a folder).
    /// </summary>
    byte[]? GetIconPng(string path, int size);
}
