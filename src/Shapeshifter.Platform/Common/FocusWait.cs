namespace Shapeshifter.Platform.Common;

/// <summary>Waits for a focus change, which the window manager carries out asynchronously.</summary>
internal static class FocusWait
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(600);

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(15);

    public static async Task<bool> UntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (true)
        {
            if (condition())
            {
                return true;
            }

            if (Environment.TickCount64 > deadline)
            {
                return false;
            }

            await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
        }
    }
}
