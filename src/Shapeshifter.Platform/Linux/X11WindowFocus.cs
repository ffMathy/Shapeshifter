using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.Linux;

/// <summary>
/// The active window, and moving the focus between the picker and the window the user was working in, through the
/// window manager (<c>_NET_ACTIVE_WINDOW</c>), with the X input focus as the fallback.
/// </summary>
internal sealed class X11WindowFocus(X11Integration x11) : IWindowFocus
{
    public FocusedWindow? Capture()
    {
        var query = x11.GetFocusedWindowAsync();
        return query.Wait(TimeSpan.FromSeconds(1)) && query.Result != IntPtr.Zero ? new FocusedWindow(query.Result) : null;
    }

    public async Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default)
    {
        // Ask the window manager first, so it raises the window and updates its own state.
        await x11.RequestActivationAsync(window.Handle).ConfigureAwait(false);
        if (await WaitForFocusAsync(window.Handle, FocusWait.Timeout, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        // Without a window manager (or one that refused), set the focus directly.
        await x11.SetInputFocusAsync(window.Handle).ConfigureAwait(false);
        return await WaitForFocusAsync(window.Handle, TimeSpan.FromMilliseconds(300), cancellationToken).ConfigureAwait(false);
    }

    public void ActivateOwnWindow(nint nativeHandle)
    {
        _ = x11.RequestActivationAsync(nativeHandle);
        _ = x11.SetInputFocusAsync(nativeHandle);
    }

    private Task<bool> WaitForFocusAsync(IntPtr window, TimeSpan timeout, CancellationToken cancellationToken) =>
        FocusWait.UntilAsync(
            () =>
            {
                var focused = x11.GetFocusedWindowAsync();
                return focused.Wait(TimeSpan.FromSeconds(1), cancellationToken) && focused.Result == window;
            },
            timeout,
            cancellationToken);
}
