using System.Runtime.Versioning;
using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.Windows;

/// <summary>
/// The foreground window, and moving the foreground between the picker and the window the user was working in.
/// </summary>
/// <remarks>
/// Windows only lets the foreground process bring another window to the foreground. Shapeshifter is not in the
/// foreground when the user starts a search (it only watches the keyboard), so to activate the picker it briefly
/// attaches its input to the foreground window's thread, which Windows treats as permission. While the picker is
/// active Shapeshifter is the foreground process, so giving the focus back needs no such trick, but it uses the same
/// path in case the user's click or another application got in between.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowFocus(ILogger logger) : IWindowFocus
{
    private readonly ILogger logger = logger.ForContext<WindowsWindowFocus>();

    public FocusedWindow? Capture()
    {
        var window = WindowsNative.GetForegroundWindow();
        if (window == 0)
        {
            return null;
        }

        WindowsNative.GetWindowThreadProcessId(window, out var processId);
        return processId == (uint)Environment.ProcessId ? null : new FocusedWindow(window);
    }

    public async Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default)
    {
        if (!WindowsNative.IsWindow(window.Handle))
        {
            return false;
        }

        var accepted = BringToForeground(window.Handle);
        if (await FocusWait.UntilAsync(() => WindowsNative.GetForegroundWindow() == window.Handle, FocusWait.Timeout, cancellationToken)
            .ConfigureAwait(false))
        {
            return true;
        }

        var foreground = WindowsNative.GetForegroundWindow();
        WindowsNative.GetWindowThreadProcessId(foreground, out var processId);
        logger.Warning(
            "Giving the foreground back to window {Window:X} failed (SetForegroundWindow returned {Accepted}); window {Foreground:X} of process {ProcessId} has it (Shapeshifter is {Own})",
            window.Handle,
            accepted,
            foreground,
            processId,
            Environment.ProcessId);
        return false;
    }

    public void ActivateOwnWindow(nint nativeHandle)
    {
        if (!BringToForeground(nativeHandle))
        {
            logger.Debug("SetForegroundWindow refused to activate the picker");
        }

        WindowsNative.SetFocus(nativeHandle);
    }

    private static bool BringToForeground(nint window)
    {
        var foreground = WindowsNative.GetForegroundWindow();
        var foregroundThread = foreground == 0 ? 0 : WindowsNative.GetWindowThreadProcessId(foreground, out _);
        var currentThread = WindowsNative.GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != currentThread
            && WindowsNative.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            if (WindowsNative.IsIconic(window))
            {
                WindowsNative.ShowWindow(window, WindowsNative.SwRestore);
            }

            WindowsNative.BringWindowToTop(window);
            return WindowsNative.SetForegroundWindow(window);
        }
        finally
        {
            if (attached)
            {
                WindowsNative.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }
}
