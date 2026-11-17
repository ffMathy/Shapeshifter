using Serilog;

namespace Shapeshifter.Core.Previews;

/// <summary>
/// Loads the images that copied links and paths point to (<see cref="LinkedImage"/>), for the picker to show behind
/// them (issue #21). The most recent images are kept in memory, so moving back and forth in the picker loads each once.
/// </summary>
/// <remarks>
/// Loading a web link sends a request to its site, so the picker only asks for the image of the item the user has
/// selected, which it shows in the preview; the rows keep their text icon. An image that cannot be loaded (no network, not an
/// image, too large) is remembered as missing as well, and the text is then shown as before.
/// </remarks>
public sealed class LinkedImageLoader
{
    /// <summary>Largest image that is loaded for a preview.</summary>
    public const long MaxImageBytes = 10 * 1024 * 1024;

    /// <summary>How many images (or failures to load one) are remembered.</summary>
    public const int CacheCapacity = 16;

    /// <summary>How long a download may take before the text is shown without its image.</summary>
    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient http;
    private readonly ILogger logger;
    private readonly Lock gate = new();
    private readonly Dictionary<string, LinkedListNode<(string Key, Task<byte[]?> Load)>> entries = [];
    private readonly LinkedList<(string Key, Task<byte[]?> Load)> recentlyUsed = [];

    public LinkedImageLoader(HttpClient http, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        this.http = http;
        this.logger = (logger ?? Serilog.Core.Logger.None).ForContext<LinkedImageLoader>();
    }

    /// <summary>
    /// The bytes of the image, or null when it cannot be loaded. Callers that load the same image at the same time share
    /// one download; cancelling only stops the wait, so the download still completes for the next caller.
    /// </summary>
    public Task<byte[]?> LoadAsync(LinkedImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        Task<byte[]?> load;
        lock (gate)
        {
            if (entries.TryGetValue(image.Key, out var node))
            {
                recentlyUsed.Remove(node);
                recentlyUsed.AddFirst(node);
                load = node.Value.Load;
            }
            else
            {
                // Never on the caller's thread: reading a file on a network drive or resolving a host can block.
                load = Task.Run(() => LoadUncachedAsync(image));
                entries[image.Key] = recentlyUsed.AddFirst((image.Key, load));
                while (recentlyUsed.Count > CacheCapacity)
                {
                    entries.Remove(recentlyUsed.Last!.Value.Key);
                    recentlyUsed.RemoveLast();
                }
            }
        }

        return load.WaitAsync(cancellationToken);
    }

    /// <summary>Whether the image has been loaded already (successfully or not), so it can be shown without a request.</summary>
    public bool TryGetLoaded(LinkedImage image, out byte[]? bytes)
    {
        ArgumentNullException.ThrowIfNull(image);
        lock (gate)
        {
            if (entries.TryGetValue(image.Key, out var node) && node.Value.Load.IsCompletedSuccessfully)
            {
                bytes = node.Value.Load.Result;
                return true;
            }
        }

        bytes = null;
        return false;
    }

    private async Task<byte[]?> LoadUncachedAsync(LinkedImage image)
    {
        try
        {
            if (image.Link is { } link)
            {
                using var timeout = new CancellationTokenSource(DownloadTimeout);
                return await ImageDownload.DownloadAsync(http, link, MaxImageBytes, timeout.Token).ConfigureAwait(false);
            }

            var file = new FileInfo(image.FilePath!);
            if (!file.Exists || file.Length > MaxImageBytes)
            {
                return null;
            }

            return await File.ReadAllBytesAsync(file.FullName).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or OperationCanceledException or IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            logger.Debug(exception, "Could not load the image of a copied link or path");
            return null;
        }
    }
}
