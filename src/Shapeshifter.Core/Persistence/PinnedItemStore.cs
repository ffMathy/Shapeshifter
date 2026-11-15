using System.Text;
using System.Text.Json;
using Serilog;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Persistence;

/// <summary>
/// Stores pinned items on disk: one folder per item containing <c>manifest.json</c> and one file per
/// clipboard part. Formats are keyed by name, never by a platform-specific numeric id.
/// </summary>
public sealed class PinnedItemStore
{
    private const int CurrentVersion = 1;
    private const string ManifestFileName = "manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string root;
    private readonly ILogger logger;

    public PinnedItemStore(string root, ILogger? logger = null)
    {
        this.root = root;
        this.logger = logger ?? Log.Logger;
    }

    public async Task SaveAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var folder = GetFolder(item.Id);
        var staging = folder + ".saving";
        if (Directory.Exists(staging))
        {
            Directory.Delete(staging, recursive: true);
        }

        Directory.CreateDirectory(staging);
        var snapshot = item.Snapshot;
        var manifest = new Manifest
        {
            Version = CurrentVersion,
            Id = item.Id,
            CapturedAt = item.CapturedAt,
            Files = [.. snapshot.Files],
        };

        if (snapshot.Text is not null)
        {
            manifest.TextFile = "text.txt";
            await File.WriteAllTextAsync(Path.Combine(staging, manifest.TextFile), snapshot.Text, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        if (snapshot.Image is not null)
        {
            manifest.ImageFile = "image.bin";
            await File.WriteAllBytesAsync(Path.Combine(staging, manifest.ImageFile), snapshot.Image, cancellationToken).ConfigureAwait(false);
        }

        for (var i = 0; i < snapshot.RawFormats.Count; i++)
        {
            var format = snapshot.RawFormats[i];
            var fileName = $"raw-{i:D3}.bin";
            manifest.RawFormats.Add(new ManifestFormat { Name = format.Name, File = fileName });
            await File.WriteAllBytesAsync(Path.Combine(staging, fileName), format.Data, cancellationToken).ConfigureAwait(false);
        }

        await File.WriteAllTextAsync(
            Path.Combine(staging, ManifestFileName),
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken).ConfigureAwait(false);

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        Directory.Move(staging, folder);
    }

    public Task DeleteAsync(Guid id)
    {
        var folder = GetFolder(id);
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.Warning(exception, "Could not delete pinned item {Id}", id);
        }

        return Task.CompletedTask;
    }

    /// <summary>Loads every pinned item, oldest pin first. Unreadable folders are skipped and logged.</summary>
    public async Task<IReadOnlyList<ClipboardItem>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var loaded = new List<(ClipboardItem Item, DateTime PinnedAt)>();
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            if (folder.EndsWith(".saving", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var item = await LoadAsync(folder, cancellationToken).ConfigureAwait(false);
                if (item is not null)
                {
                    loaded.Add((item, Directory.GetLastWriteTimeUtc(folder)));
                }
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or FormatException)
            {
                logger.Warning(exception, "Skipping unreadable pinned item in {Folder}", folder);
            }
        }

        return loaded.OrderBy(entry => entry.PinnedAt).Select(entry => entry.Item).ToArray();
    }

    private async Task<ClipboardItem?> LoadAsync(string folder, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(folder, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = JsonSerializer.Deserialize<Manifest>(
            await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false), JsonOptions);
        if (manifest is null || manifest.Version > CurrentVersion)
        {
            return null;
        }

        var raw = new List<RawClipboardFormat>();
        foreach (var format in manifest.RawFormats)
        {
            raw.Add(new RawClipboardFormat(format.Name, await File.ReadAllBytesAsync(Resolve(folder, format.File), cancellationToken).ConfigureAwait(false)));
        }

        var snapshot = new ClipboardSnapshot
        {
            Text = manifest.TextFile is null ? null : await File.ReadAllTextAsync(Resolve(folder, manifest.TextFile), Encoding.UTF8, cancellationToken).ConfigureAwait(false),
            Image = manifest.ImageFile is null ? null : await File.ReadAllBytesAsync(Resolve(folder, manifest.ImageFile), cancellationToken).ConfigureAwait(false),
            Files = manifest.Files,
            RawFormats = raw,
        };

        return new ClipboardItem(snapshot, manifest.Id, manifest.CapturedAt);
    }

    private string GetFolder(Guid id) => Path.Combine(root, id.ToString("N"));

    /// <summary>Resolves a manifest-relative file name, refusing anything that escapes the item folder.</summary>
    private static string Resolve(string folder, string fileName)
    {
        var full = Path.GetFullPath(Path.Combine(folder, fileName));
        var folderFull = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(folderFull, StringComparison.Ordinal))
        {
            throw new FormatException($"Invalid file reference '{fileName}' in pinned item manifest.");
        }

        return full;
    }

    private sealed class Manifest
    {
        public int Version { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset CapturedAt { get; set; }

        public string? TextFile { get; set; }

        public string? ImageFile { get; set; }

        public List<string> Files { get; set; } = [];

        public List<ManifestFormat> RawFormats { get; set; } = [];
    }

    private sealed class ManifestFormat
    {
        public string Name { get; set; } = string.Empty;

        public string File { get; set; } = string.Empty;
    }
}
