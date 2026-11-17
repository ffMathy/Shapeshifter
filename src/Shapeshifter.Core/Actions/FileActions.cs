using System.Globalization;
using System.IO.Compression;
using System.Text;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Actions;

/// <summary>
/// Saves copied text to a .txt file, or a copied image to an image file, and pastes that file as if the file had been
/// copied.
/// </summary>
public sealed class PasteAsFileAction : IClipboardAction
{
    public string Id => "paste-as-file";

    public int Order => 10;

    public string GetTitle(ClipboardItem item) => Strings.ActionPasteAsFile;

    public string GetDescription(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return IsImage(item)
            ? Localizer.Format(Strings.ActionPasteAsImageFileDescription, ImageFormat.GetExtension(item.Snapshot.Image!))
            : Strings.ActionPasteAsFileDescription;
    }

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return IsImage(item) || (item.Kind == ClipboardContentKind.Text && !string.IsNullOrEmpty(item.Snapshot.Text));
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        var image = IsImage(item);

        // One folder per item, so items captured in the same second never overwrite each other's file.
        var folder = Path.Combine(context.Paths.TempDirectory, image ? "Image files" : "Text files", item.Id.ToString("N"));
        Directory.CreateDirectory(folder);
        var time = item.CapturedAt.LocalDateTime.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture);
        string path;
        if (image)
        {
            var bytes = item.Snapshot.Image!;
            path = Path.Combine(folder, $"Image {time}.{ImageFormat.GetExtension(bytes)}");
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            path = Path.Combine(folder, $"Text {time}.txt");
            await File.WriteAllTextAsync(path, item.Snapshot.Text, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        await context.WriteAndPasteAsync(new ClipboardSnapshot { Files = [path] }, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsImage(ClipboardItem item) =>
        item.Kind == ClipboardContentKind.Image && item.Snapshot.Image is { Length: > 0 };
}

/// <summary>Compresses the copied files and folders into a .zip archive and copies the archive.</summary>
public sealed class ZipFilesAction : IClipboardAction
{
    public string Id => "copy-as-zip";

    public int Order => 75;

    public string GetTitle(ClipboardItem item) => Strings.ActionCopyAsZip;

    public string GetDescription(ClipboardItem item) => Strings.ActionCopyAsZipDescription;

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Not for an image that merely comes with a temporary copy of itself (Firefox), which is gone soon.
        return item.Kind is ClipboardContentKind.File or ClipboardContentKind.FileCollection;
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        var archive = await Task.Run(() => CreateArchive(item.Snapshot.Files, context.Paths.TempDirectory, cancellationToken), cancellationToken).ConfigureAwait(false);
        await context.CopyNewContentAsync(new ClipboardSnapshot { Files = [archive] }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates the archive and returns its path. Folders are added recursively.</summary>
    public static string CreateArchive(IReadOnlyList<string> paths, string temporaryRoot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var name = GetArchiveName(paths);
        var folder = Path.Combine(temporaryRoot, "Compressed folders", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(folder);
        var archivePath = Path.Combine(folder, name + ".zip");

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                archive.CreateEntryFromFile(path, Unique(Path.GetFileName(path), usedNames), CompressionLevel.Optimal);
            }
            else if (Directory.Exists(path))
            {
                var directoryName = Unique(Path.GetFileName(Path.TrimEndingDirectorySeparator(path)), usedNames);
                // Symbolic links and junctions are not followed: they can form cycles or point outside the folder.
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    IgnoreInaccessible = true,
                };
                foreach (var file in Directory.EnumerateFiles(path, "*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relative = Path.GetRelativePath(path, file).Replace(Path.DirectorySeparatorChar, '/');
                    archive.CreateEntryFromFile(file, directoryName + "/" + relative, CompressionLevel.Optimal);
                }
            }
        }

        return archivePath;
    }

    /// <summary>The single item's name, or the name of the folder that contains all items.</summary>
    public static string GetArchiveName(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 1)
        {
            return Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(paths[0])) is { Length: > 0 } single ? single : "Archive";
        }

        var parents = paths.Select(p => Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(p)) ?? string.Empty).Distinct().ToArray();
        var parentName = parents.Length == 1 ? Path.GetFileName(parents[0]) : null;
        return string.IsNullOrEmpty(parentName) ? "Archive" : parentName;
    }

    private static string Unique(string name, HashSet<string> usedNames)
    {
        var candidate = name;
        for (var i = 2; !usedNames.Add(candidate); i++)
        {
            candidate = $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}";
        }

        return candidate;
    }
}
