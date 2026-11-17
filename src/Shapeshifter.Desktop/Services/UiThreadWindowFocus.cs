using Avalonia.Threading;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Desktop.Services;

/// <summary>Moves the keyboard focus from the UI thread, whatever thread asks.</summary>
/// <remarks>
/// The engine gives the focus back from its effect loop, on a thread-pool thread, while the picker (on the UI thread)
/// still has it. Windows only reliably lets the thread whose window is in the foreground hand the foreground on: from
/// another thread, <c>SetForegroundWindow</c> was refused at times, the window the user searched from never got the
/// focus back, and the paste was lost (the search end-to-end test failed now and then on Windows). On the UI thread the
/// restore also waits until the picker has finished handling the key that ended the search.
/// </remarks>
internal sealed class UiThreadWindowFocus(IWindowFocus inner) : IWindowFocus
{
    public FocusedWindow? Capture() => inner.Capture();

    public Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default) =>
        Dispatcher.UIThread.CheckAccess()
            ? inner.RestoreAsync(window, cancellationToken)
            : Dispatcher.UIThread.InvokeAsync(() => inner.RestoreAsync(window, cancellationToken));

    public void ActivateOwnWindow(nint nativeHandle) => inner.ActivateOwnWindow(nativeHandle);
}
