using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Decodes and caches the images the picker shows next to items: the icons of the applications items were copied
/// from, and the operating system's icons for copied files and their folders.
/// </summary>
/// <remarks>Used on the UI thread only.</remarks>
public sealed class PickerVisuals
{
    private const int MaxFileIcons = 256;

    private readonly IFileIconProvider? fileIcons;
    private readonly ILogger logger;
    private readonly ConditionalWeakTable<byte[], Bitmap?> sourceIcons = [];
    private readonly Dictionary<(string Key, int Size), Bitmap?> fileIconCache = [];

    public PickerVisuals(IFileIconProvider? fileIcons, ILogger logger)
    {
        this.fileIcons = fileIcons;
        this.logger = logger;
    }

    /// <summary>The icon of the application an item was copied from.</summary>
    public Bitmap? GetSourceIcon(ClipboardSource? source)
    {
        if (source?.IconPng is not { Length: > 0 } png)
        {
            return null;
        }

        return sourceIcons.GetValue(png, Decode);
    }

    /// <summary>The icon of a file or folder, as the file manager shows it; <see langword="null"/> if unavailable.</summary>
    public Bitmap? GetFileIcon(string path, int size)
    {
        if (fileIcons is null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        var key = (GetIconKey(path), size);
        if (fileIconCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        Bitmap? bitmap = null;
        try
        {
            if (fileIcons.GetIconPng(path, size) is { Length: > 0 } png)
            {
                bitmap = Decode(png);
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not get the icon of {Path}", path);
        }

        if (fileIconCache.Count >= MaxFileIcons)
        {
            fileIconCache.Clear();
        }

        fileIconCache[key] = bitmap;
        return bitmap;
    }

    /// <summary>
    /// Files that share an icon share a key: folders, and files with the same extension, except for files whose icon
    /// is their own (programs, shortcuts, icons).
    /// </summary>
    public static string GetIconKey(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.EndsWith(Path.AltDirectorySeparatorChar) || FileSystemProbe.IsDirectory(path))
        {
            return "<folder>";
        }

        var extension = Path.GetExtension(path).ToUpperInvariant();
        return extension is ".EXE" or ".LNK" or ".ICO" or ".APP" or ".DESKTOP" or ".URL" ? path : extension;
    }

    private Bitmap? Decode(byte[] png)
    {
        try
        {
            using var stream = new MemoryStream(png);
            return new Bitmap(stream);
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not decode an icon");
            return null;
        }
    }
}
