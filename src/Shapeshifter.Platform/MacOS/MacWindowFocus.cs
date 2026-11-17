using System.Runtime.Versioning;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.MacOS;

/// <summary>
/// The frontmost application, and moving the focus between the picker and the application the user was working in.
/// </summary>
/// <remarks>
/// macOS gives the keyboard focus to applications rather than windows, and brings the application's key window back
/// with it, so the application's process id is all that has to be remembered.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacWindowFocus : IWindowFocus
{
    /// <summary><c>NSApplicationActivateIgnoringOtherApps</c>.</summary>
    private const nuint ActivateIgnoringOtherApps = 1 << 1;

    public FocusedWindow? Capture()
    {
        var pid = InPool(GetFrontmostProcessId);
        return pid > 0 && pid != Environment.ProcessId ? new FocusedWindow(pid) : null;
    }

    public async Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default)
    {
        var requested = InPool(() =>
        {
            var applicationClass = MacNative.objc_getClass("NSRunningApplication");
            if (applicationClass == IntPtr.Zero)
            {
                return false;
            }

            var application = MacNative.SendIntPtrInt32(
                applicationClass, MacNative.sel_registerName("runningApplicationWithProcessIdentifier:"), (int)window.Handle);
            return application != IntPtr.Zero
                && MacNative.SendByteNUInt(application, MacNative.sel_registerName("activateWithOptions:"), ActivateIgnoringOtherApps) != 0;
        });

        return requested && await FocusWait.UntilAsync(() => InPool(GetFrontmostProcessId) == window.Handle, FocusWait.Timeout, cancellationToken)
            .ConfigureAwait(false);
    }

    public void ActivateOwnWindow(nint nativeHandle) => InPool(() =>
    {
        var applicationClass = MacNative.objc_getClass("NSApplication");
        if (applicationClass != IntPtr.Zero)
        {
            var application = MacNative.SendIntPtr(applicationClass, MacNative.sel_registerName("sharedApplication"));
            MacNative.SendByteByte(application, MacNative.sel_registerName("activateIgnoringOtherApps:"), 1);
        }

        if (nativeHandle != IntPtr.Zero)
        {
            MacNative.SendIntPtrIntPtr(nativeHandle, MacNative.sel_registerName("makeKeyAndOrderFront:"), IntPtr.Zero);
        }

        return true;
    });

    private static int GetFrontmostProcessId()
    {
        if (!MacNative.TryLoadAppKit())
        {
            return 0;
        }

        var workspaceClass = MacNative.objc_getClass("NSWorkspace");
        if (workspaceClass == IntPtr.Zero)
        {
            return 0;
        }

        var workspace = MacNative.SendIntPtr(workspaceClass, MacNative.sel_registerName("sharedWorkspace"));
        var application = workspace == IntPtr.Zero ? IntPtr.Zero : MacNative.SendIntPtr(workspace, MacNative.sel_registerName("frontmostApplication"));
        return application == IntPtr.Zero ? 0 : MacNative.SendInt32(application, MacNative.sel_registerName("processIdentifier"));
    }

    private static T InPool<T>(Func<T> work)
    {
        var pool = MacNative.objc_autoreleasePoolPush();
        try
        {
            return work();
        }
        finally
        {
            MacNative.objc_autoreleasePoolPop(pool);
        }
    }
}
