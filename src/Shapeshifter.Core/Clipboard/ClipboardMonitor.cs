using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Clipboard;

public sealed class ClipboardCapturedEventArgs(ClipboardItem item, bool isNew) : EventArgs
{
    public ClipboardItem Item { get; } = item;

    /// <summary><see langword="false"/> when the content was already in the history and was only moved to the top.</summary>
    public bool IsNew { get; } = isNew;
}

/// <summary>
/// Captures clipboard changes into the <see cref="ClipboardHistory"/>.
/// </summary>
/// <remarks>
/// Change notifications are debounced (applications often write the clipboard several times per copy) and
/// captures are serialized. Content that Shapeshifter wrote itself is recognised by its fingerprint and ignored,
/// which is robust on polling platforms where a "skip the next notification" flag would race.
/// </remarks>
public sealed class ClipboardMonitor : IDisposable
{
    private readonly IClipboardBackend backend;
    private readonly IClipboardChangeSource changeSource;
    private readonly ClipboardHistory history;
    private readonly ILogger logger;
    private readonly SemaphoreSlim captureLock = new(1, 1);
    private readonly Lock gate = new();
    private CancellationTokenSource? pendingCapture;
    private string? selfWrittenFingerprint;
    private long selfWriteGraceEndsAt;
    private bool started;
    private bool disposed;

    public ClipboardMonitor(
        IClipboardBackend backend,
        IClipboardChangeSource changeSource,
        ClipboardHistory history,
        ILogger? logger = null)
    {
        this.backend = backend;
        this.changeSource = changeSource;
        this.history = history;
        this.logger = logger ?? Log.Logger;
    }

    public event EventHandler<ClipboardCapturedEventArgs>? Captured;

    public TimeSpan Debounce { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Any change shortly after Shapeshifter wrote the clipboard is attributed to that write. Some platforms
    /// re-encode data (for example images) so the read-back is not byte-identical to what was written.
    /// </summary>
    public TimeSpan SelfWriteGracePeriod { get; set; } = TimeSpan.FromMilliseconds(750);

    public void Start()
    {
        if (started)
        {
            return;
        }

        started = true;
        changeSource.Changed += OnClipboardChanged;
        changeSource.Start();
    }

    /// <summary>
    /// Writes <paramref name="snapshot"/> to the clipboard without capturing it as a new copy.
    /// </summary>
    public async Task WriteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (gate)
        {
            selfWrittenFingerprint = snapshot.Fingerprint;
            selfWriteGraceEndsAt = long.MaxValue;
        }

        try
        {
            await backend.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
            {
                selfWriteGraceEndsAt = Environment.TickCount64 + (long)SelfWriteGracePeriod.TotalMilliseconds;
            }
        }
    }

    /// <summary>Captures the current clipboard immediately (also used once at startup).</summary>
    public async Task<ClipboardItem?> CaptureNowAsync(CancellationToken cancellationToken = default)
    {
        await captureLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = await backend.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (snapshot.IsEmpty)
            {
                return null;
            }

            lock (gate)
            {
                if (snapshot.Fingerprint == selfWrittenFingerprint || Environment.TickCount64 < selfWriteGraceEndsAt)
                {
                    selfWrittenFingerprint = snapshot.Fingerprint;
                    logger.Debug("Ignoring clipboard change caused by Shapeshifter itself");
                    return null;
                }

                selfWrittenFingerprint = null;
            }

            var item = history.Add(snapshot, out var isNew);
            logger.Information("Captured {Kind} item ({State}, {Size} bytes)", item.Kind, isNew ? "new" : "existing", snapshot.ApproximateSize);
            Captured?.Invoke(this, new ClipboardCapturedEventArgs(item, isNew));
            return item;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || disposed)
        {
            return null;
        }
        catch (Exception exception)
        {
            logger.Warning(exception, "Could not read the clipboard");
            return null;
        }
        finally
        {
            captureLock.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        changeSource.Changed -= OnClipboardChanged;
        lock (gate)
        {
            pendingCapture?.Cancel();
            pendingCapture?.Dispose();
            pendingCapture = null;
        }
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        CancellationTokenSource cancellation;
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            pendingCapture?.Cancel();
            pendingCapture?.Dispose();
            pendingCapture = cancellation = new CancellationTokenSource();
        }

        _ = CaptureAfterDebounceAsync(cancellation.Token);
    }

    private async Task CaptureAfterDebounceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Debounce, cancellationToken).ConfigureAwait(false);
            await CaptureNowAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A newer change superseded this one.
        }
    }
}
