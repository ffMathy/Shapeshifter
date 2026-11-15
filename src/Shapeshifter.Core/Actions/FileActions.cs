using System.IO.Compression;
using System.Text;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Actions;

/// <summary>Saves copied text to a .txt file and copies that file.</summary>
public sealed class CopyAsFileAction : IClipboardAction
{
    public string Id => "copy-as-file";

    public int Order => 20;

    public string GetTitle(ClipboardItem item) => "Copy as file";

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind == ClipboardContentKind.Text && !string.IsNullOrEmpty(item.Snapshot.Text);
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        // One folder per item, so items captured in the same second never overwrite each other's file.
        var folder = Path.Combine(context.Paths.TempDirectory, "Text files", item.Id.ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Text {item.CapturedAt.LocalDateTime:yyyy-MM-dd HH.mm.ss}.txt");
        await File.WriteAllTextAsync(path, item.Snapshot.Text, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await context.CopyNewContentAsync(new ClipboardSnapshot { Files = [path] }, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Compresses the copied files and folders into a .zip archive and copies the archive.</summary>
public sealed class ZipFilesAction : IClipboardAction
{
    public string Id => "copy-as-zip";

    public int Order => 75;

    public string GetTitle(ClipboardItem item) => "Copy as compressed folder";

    public bool CanExecute(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Snapshot.Files.Count > 0;
    }

    public async Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        var archive = await Task.Run(() => CreateArchive(item.Snapshot.Files, context.Paths.TempDirectory), cancellationToken).ConfigureAwait(false);
        await context.CopyNewContentAsync(new ClipboardSnapshot { Files = [archive] }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates the archive and returns its path. Folders are added recursively.</summary>
    public static string CreateArchive(IReadOnlyList<string> paths, string temporaryRoot)
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
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
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
