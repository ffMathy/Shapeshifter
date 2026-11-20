using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Reads and writes the system clipboard through Avalonia, which talks to Win32/OLE, NSPasteboard or the
/// X11 selection protocol. Text, files and images go through Avalonia's portable formats; every other
/// format is copied as raw bytes so it can be restored exactly.
/// </summary>
internal sealed class AvaloniaClipboardBackend : IClipboardBackend
{
    private readonly TopLevel host;
    private readonly ILogger logger;

    // What Shapeshifter last put on the clipboard. On X11 the clipboard hands this very object back while Shapeshifter
    // owns the selection, and it must stay alive (not disposed) to serve other applications' requests for it.
    private DataTransfer? written;

    public AvaloniaClipboardBackend(TopLevel host, ILogger logger)
    {
        this.host = host;
        this.logger = logger.ForContext<AvaloniaClipboardBackend>();
    }

    private IClipboard Clipboard => host.Clipboard ?? throw new InvalidOperationException("The clipboard is not available.");

    public Task<ClipboardSnapshot> ReadAsync(CancellationToken cancellationToken = default) =>
        OnUiThread(async () =>
        {
            using var flush = KeepX11RequestsFlowing();
            return await ReadCoreAsync(cancellationToken).ConfigureAwait(true);
        });

    /// <summary>
    /// On X11, Avalonia 12.1 continues a clipboard read on a thread-pool thread and sends its requests to the owner
    /// from there without flushing them. Its event loop flushes only when it wakes, and it can sleep with nothing to
    /// wake it: the request then stays in Xlib's buffer, the owner never answers, and the read waits forever (its own
    /// timeout runs on a dispatcher that never runs). A timer ticking on the UI thread while a read is under way wakes
    /// the loop, which flushes the request.
    /// </summary>
    private static IDisposable? KeepX11RequestsFlowing()
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Background, (_, _) => { });
        timer.Start();
        return new TimerStopper(timer);
    }

    private sealed class TimerStopper(DispatcherTimer timer) : IDisposable
    {
        public void Dispose() => timer.Stop();
    }

    public Task WriteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken = default) =>
        OnUiThread(async () =>
        {
            await WriteCoreAsync(snapshot).ConfigureAwait(true);
            return true;
        });

    public Task<string> GetChangeTokenAsync(CancellationToken cancellationToken = default) =>
        OnUiThread(async () =>
        {
            var data = await Clipboard.TryGetDataAsync().ConfigureAwait(true);
            if (data is null)
            {
                return string.Empty;
            }

            using var owned = IsWritten(data) ? null : data;

            // The poller only compares tokens for equality, so the content itself is a cheaper and exact token:
            // a string comparison stops at the first difference, while hashing reads and copies all of it.
            var formats = string.Join('|', data.Formats.Select(format => format.Identifier));
            var text = data.Contains(DataFormat.Text) ? await data.TryGetTextAsync().ConfigureAwait(true) : null;
            return string.Concat(formats, "|", text);
        });

    private static async Task<T> OnUiThread<T>(Func<Task<T>> work)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return await work().ConfigureAwait(true);
        }

        return await Dispatcher.UIThread.InvokeAsync(work).ConfigureAwait(false);
    }

    private async Task<ClipboardSnapshot> ReadCoreAsync(CancellationToken cancellationToken)
    {
        var data = await Clipboard.TryGetDataAsync().ConfigureAwait(true);

        // Shapeshifter's own write is read like any other copy, but never disposed, nor is its bitmap: other
        // applications are still to be served from them (a paste of an image from the history crashed as soon as
        // another application asked for it).
        var own = IsWritten(data);
        using var owned = own ? null : data;
        if (data is null || data.Formats.Count == 0)
        {
            return ClipboardSnapshot.Empty;
        }

        // Respect password managers and other applications that ask not to be recorded, before reading anything else.
        var formatNames = data.Formats.Select(format => format.Identifier).ToArray();
        if (await ClipboardFormatClassifier.IsExcludedFromHistoryAsync(formatNames, name => ReadMarkerAsync(data, name)).ConfigureAwait(true))
        {
            logger.Information("Skipping clipboard content that the source application marked as sensitive");
            return ClipboardSnapshot.Empty;
        }

        string? text = null;
        if (data.Contains(DataFormat.Text))
        {
            text = await TryAsync(() => data.TryGetTextAsync(), "text").ConfigureAwait(true);
        }

        IReadOnlyList<string> files = [];
        if (data.Contains(DataFormat.File))
        {
            var items = await TryAsync(() => data.TryGetFilesAsync(), "files").ConfigureAwait(true);
            files = items?.Select(item => item.TryGetLocalPath()).OfType<string>().ToArray() ?? [];
        }

        byte[]? image = null;
        if (data.Contains(DataFormat.Bitmap))
        {
            var bitmap = await TryAsync(() => data.TryGetBitmapAsync(), "image").ConfigureAwait(true);
            using var ownedBitmap = own ? null : bitmap;
            if (bitmap is not null)
            {
                using var stream = new MemoryStream();
                bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                image = stream.ToArray();
            }
        }

        var raw = new List<RawClipboardFormat>();
        foreach (var format in data.Formats)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (format.Kind != DataFormatKind.Platform || !ClipboardFormatClassifier.ShouldCapture(format.Identifier))
            {
                continue;
            }

            // On Windows, Avalonia names predefined formats it has no name for "Unknown_Format_<id>"
            // (CF_LOCALE, CF_ENHMETAFILE, CF_METAFILEPICT, CF_PALETTE, ...). The GDI-handle ones are not
            // HGLOBAL memory and so slip past the ignore list under their CF_* names, and none of them can be
            // written back by name, so skip them instead of reading and re-registering a bogus format.
            if (OperatingSystem.IsWindows() && format.Identifier.StartsWith("Unknown_Format_", StringComparison.Ordinal))
            {
                continue;
            }

            var item = data.Items.FirstOrDefault(candidate => candidate.Formats.Contains(format));
            if (item is null)
            {
                continue;
            }

            var value = await TryAsync(() => item.TryGetRawAsync(format), format.Identifier).ConfigureAwait(true);
            var bytes = value switch
            {
                byte[] array => array,
                string str => Encoding.UTF8.GetBytes(str),
                _ => null,
            };

            if (bytes is { Length: > 0 } && ClipboardFormatClassifier.ShouldCapture(format.Identifier, bytes.Length))
            {
                raw.Add(new RawClipboardFormat(format.Identifier, bytes));
            }
        }

        return new ClipboardSnapshot { Text = text, Files = files, Image = image, RawFormats = raw };
    }

    private async Task<byte[]?> ReadMarkerAsync(IAsyncDataTransfer data, string formatName)
    {
        var format = data.Formats.First(candidate => candidate.Identifier == formatName);
        var item = data.Items.FirstOrDefault(candidate => candidate.Formats.Contains(format));
        if (item is null)
        {
            return null;
        }

        return await TryAsync(() => item.TryGetRawAsync(format), formatName).ConfigureAwait(true) switch
        {
            byte[] bytes => bytes,
            string text => Encoding.UTF8.GetBytes(text),
            _ => null,
        };
    }

    private async Task WriteCoreAsync(ClipboardSnapshot snapshot)
    {
        var transfer = new DataTransfer();
        var main = new DataTransferItem();

        if (snapshot.Text is not null)
        {
            main.SetText(snapshot.Text);
        }

        Bitmap? bitmap = null;
        if (snapshot.Image is not null)
        {
            try
            {
                using var stream = new MemoryStream(snapshot.Image);
                bitmap = new Bitmap(stream);
                main.SetBitmap(bitmap);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
            {
                logger.Warning(exception, "Could not decode the image to put on the clipboard");
            }
        }

        foreach (var format in snapshot.RawFormats)
        {
            // If the image could not be decoded, its raw image formats are the next best thing.
            var restoreAnyway = bitmap is null && ClipboardFormatClassifier.IsImage(format.Name) && ClipboardFormatClassifier.ShouldCapture(format.Name);
            if (ClipboardFormatClassifier.ShouldRestore(format.Name, snapshot) || restoreAnyway)
            {
                main.Set(DataFormat.CreateBytesPlatformFormat(format.Name), format.Data);
            }
        }

        var storageItems = new List<IStorageItem>();
        foreach (var path in snapshot.Files)
        {
            var uri = new Uri(path);
            IStorageItem? storageItem = Directory.Exists(path)
                ? await host.StorageProvider.TryGetFolderFromPathAsync(uri).ConfigureAwait(true)
                : await host.StorageProvider.TryGetFileFromPathAsync(uri).ConfigureAwait(true);
            if (storageItem is null)
            {
                logger.Warning("Copied file {Path} no longer exists", path);
                continue;
            }

            storageItems.Add(storageItem);
        }

        if (storageItems.Count > 0)
        {
            main.SetFile(storageItems[0]);
        }

        transfer.Add(main);
        foreach (var storageItem in storageItems.Skip(1))
        {
            transfer.Add(DataTransferItem.CreateFile(storageItem));
        }

        written = transfer;
        await Clipboard.SetDataAsync(transfer).ConfigureAwait(true);
        if (OperatingSystem.IsWindows())
        {
            // Keep the data available after Shapeshifter exits.
            await Clipboard.FlushAsync().ConfigureAwait(true);
        }
    }

    private bool IsWritten(IAsyncDataTransfer? data) => data is not null && ReferenceEquals(data, written);

    private async Task<T?> TryAsync<T>(Func<Task<T?>> read, string what)
    {
        try
        {
            return await read().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not read clipboard format {Format}", what);
            return default;
        }
    }
}
