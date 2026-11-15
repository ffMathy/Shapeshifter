using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Clipboard;

/// <summary>
/// Detects clipboard changes by polling a change token, for platforms without change notifications.
/// </summary>
public sealed class PollingClipboardChangeSource : IClipboardChangeSource
{
    private readonly Func<CancellationToken, Task<string>> tokenProvider;
    private readonly TimeSpan interval;
    private readonly ILogger logger;
    private readonly CancellationTokenSource cancellation = new();
    private Task? loop;
    private int disposed;

    public PollingClipboardChangeSource(Func<CancellationToken, Task<string>> tokenProvider, TimeSpan interval, ILogger? logger = null)
    {
        this.tokenProvider = tokenProvider;
        this.interval = interval;
        this.logger = logger ?? Log.Logger;
    }

    public event EventHandler? Changed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        loop ??= Task.Run(() => PollAsync(cancellation.Token));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        cancellation.Cancel();
        try
        {
            loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation.
        }

        cancellation.Dispose();
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        string? previous = null;
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                var token = await tokenProvider(cancellationToken).ConfigureAwait(false);
                if (previous is not null && token != previous)
                {
                    Changed?.Invoke(this, EventArgs.Empty);
                }

                previous = token;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.Debug(exception, "Polling the clipboard change token failed");
            }
        }
        while (await WaitAsync(timer, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
