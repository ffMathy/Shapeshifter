using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Serilog;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Linux;

/// <summary>
/// Owns a private X11 connection on a dedicated thread. It provides what libuiohook cannot do on Linux:
/// <list type="bullet">
/// <item>suppressing the paste keystroke, by passively grabbing Ctrl+V on the root window
/// (the focused application then never receives it), and the navigation keys while a gesture is active;</item>
/// <item>clipboard change notifications through the XFixes extension;</item>
/// <item>the pointer position.</item>
/// </list>
/// Key events themselves are still observed through SharpHook (XRecord), which also sees grabbed keys.
/// </summary>
internal sealed unsafe class X11Integration : IClipboardChangeSource, ICursorLocator
{
    private static readonly uint[] LockVariants =
    [
        0,
        X11Native.LockMask,
        X11Native.Mod2Mask,
        X11Native.LockMask | X11Native.Mod2Mask,
    ];

    private static readonly ulong[] NavigationKeysyms =
    [
        X11Native.XK_Up, X11Native.XK_Down, X11Native.XK_Left, X11Native.XK_Right, X11Native.XK_Delete, X11Native.XK_Escape,
    ];

    // Process-wide: X error handlers are global, so ours is chained in front of whatever was installed before.
    private static readonly X11Native.XErrorHandler ErrorHandlerDelegate = OnXError;
    private static IntPtr previousErrorHandler;
    private static IntPtr ownDisplay;
    private static int lastOwnErrorCode;

    private readonly ILogger logger;
    private readonly ConcurrentQueue<(Action Work, TaskCompletionSource Done)> commands = new();
    private readonly Lock pointerGate = new();
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread? thread;
    private volatile bool running;
    private IntPtr display;
    private IntPtr pointerDisplay;
    private IntPtr root;
    private IntPtr clipboardAtom;
    private int xfixesEventBase = -1;
    private int triggerKeycode;
    private bool triggerGrabbed;
    private bool navigationGrabbed;
    private bool clipboardEventsRequested;

    public X11Integration(ILogger logger)
    {
        this.logger = logger.ForContext<X11Integration>();
    }

    public event EventHandler? Changed;

    public static bool IsAvailable => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    public bool SupportsClipboardNotifications => xfixesEventBase >= 0;

    /// <summary>Opens the connection and starts the event thread.</summary>
    public Task StartAsync()
    {
        if (thread is not null)
        {
            return started.Task;
        }

        running = true;
        thread = new Thread(Run) { IsBackground = true, Name = "Shapeshifter X11" };
        thread.Start();
        return started.Task;
    }

    /// <summary>Starts XFixes clipboard notifications (part of <see cref="IClipboardChangeSource"/>).</summary>
    public void Start() => _ = InvokeAsync(() =>
    {
        if (xfixesEventBase < 0 || clipboardEventsRequested)
        {
            return;
        }

        X11Native.XFixesSelectSelectionInput(display, root, clipboardAtom, X11Native.XFixesSetSelectionOwnerNotifyMask);
        X11Native.XFlush(display);
        clipboardEventsRequested = true;
    });

    public Task SetTriggerGrabbedAsync(bool grabbed) => InvokeAsync(() =>
    {
        if (grabbed == triggerGrabbed)
        {
            return;
        }

        if (grabbed)
        {
            GrabKey(triggerKeycode);
        }
        else
        {
            UngrabKey(triggerKeycode);

            // While V is still physically held, the passive grab is an *active* keyboard grab that XUngrabKey does not
            // end (for example when Ctrl was released a few milliseconds before V). Our synthetic Ctrl+V would then be
            // delivered to us instead of the focused window, so end it explicitly.
            X11Native.XUngrabKeyboard(display, X11Native.CurrentTime);
        }

        X11Native.XSync(display, false);
        triggerGrabbed = grabbed;
    });

    public Task SetNavigationGrabbedAsync(bool grabbed) => InvokeAsync(() =>
    {
        if (grabbed == navigationGrabbed)
        {
            return;
        }

        foreach (var keysym in NavigationKeysyms)
        {
            var keycode = X11Native.XKeysymToKeycode(display, keysym);
            if (keycode == 0)
            {
                continue;
            }

            if (grabbed)
            {
                GrabKey(keycode);
            }
            else
            {
                UngrabKey(keycode);
            }
        }

        X11Native.XSync(display, false);
        navigationGrabbed = grabbed;
    });

    public (int X, int Y)? GetCursorPosition()
    {
        lock (pointerGate)
        {
            if (pointerDisplay == IntPtr.Zero)
            {
                return null;
            }

            var pointerRoot = X11Native.XDefaultRootWindow(pointerDisplay);
            return X11Native.XQueryPointer(pointerDisplay, pointerRoot, out _, out _, out var x, out var y, out _, out _, out _)
                ? (x, y)
                : null;
        }
    }

    public void Dispose()
    {
        if (!running)
        {
            return;
        }

        try
        {
            SetNavigationGrabbedAsync(false).Wait(TimeSpan.FromSeconds(1));
            SetTriggerGrabbedAsync(false).Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // The connection may already be gone.
        }

        running = false;
        thread?.Join(TimeSpan.FromSeconds(2));
        lock (pointerGate)
        {
            if (pointerDisplay != IntPtr.Zero)
            {
                X11Native.XCloseDisplay(pointerDisplay);
                pointerDisplay = IntPtr.Zero;
            }
        }
    }

    private Task InvokeAsync(Action work)
    {
        if (!running)
        {
            return Task.CompletedTask;
        }

        if (Thread.CurrentThread == thread)
        {
            work();
            return Task.CompletedTask;
        }

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        commands.Enqueue((work, done));
        return done.Task;
    }

    private void Run()
    {
        var eventBuffer = Marshal.AllocHGlobal(X11Native.XEventSize);
        try
        {
            display = X11Native.XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                throw new PlatformNotSupportedException("Could not connect to the X server (is DISPLAY set?).");
            }

            lock (pointerGate)
            {
                pointerDisplay = X11Native.XOpenDisplay(IntPtr.Zero);
            }

            InstallErrorHandler(display);
            root = X11Native.XDefaultRootWindow(display);
            clipboardAtom = X11Native.XInternAtom(display, "CLIPBOARD", false);
            triggerKeycode = ResolveTriggerKeycode();
            if (X11Native.XFixesQueryExtension(display, out var eventBase, out _))
            {
                xfixesEventBase = eventBase;
            }
            else
            {
                logger.Warning("The X server has no XFIXES extension; falling back to clipboard polling");
            }

            logger.Information("Connected to X11 (trigger keycode {Keycode}, XFixes {XFixes})", triggerKeycode, xfixesEventBase >= 0);
            started.TrySetResult();

            var fd = X11Native.XConnectionNumber(display);
            while (running)
            {
                RunCommands();
                while (X11Native.XPending(display) > 0)
                {
                    X11Native.XNextEvent(display, eventBuffer);
                    HandleEvent(eventBuffer);
                }

                var poll = new X11Native.PollFd { Fd = fd, Events = X11Native.PollIn };
                X11Native.Poll(&poll, 1, 10);
            }

            RunCommands();
        }
        catch (Exception exception)
        {
            logger.Error(exception, "The X11 integration thread failed");
            started.TrySetException(exception);
        }
        finally
        {
            Marshal.FreeHGlobal(eventBuffer);
            running = false;
            while (commands.TryDequeue(out var command))
            {
                command.Done.TrySetResult();
            }

            if (display != IntPtr.Zero)
            {
                X11Native.XCloseDisplay(display);
                display = IntPtr.Zero;
            }
        }
    }

    private void RunCommands()
    {
        while (commands.TryDequeue(out var command))
        {
            try
            {
                command.Work();
                command.Done.TrySetResult();
            }
            catch (Exception exception)
            {
                command.Done.TrySetException(exception);
            }
        }
    }

    /// <summary>
    /// The keycode that produces "v" in the current layout. Falls back to 55, the physical V key on evdev-based
    /// servers, because keycode 0 would mean AnyKey and grab every Ctrl shortcut.
    /// </summary>
    private int ResolveTriggerKeycode()
    {
        var keycode = X11Native.XKeysymToKeycode(display, X11Native.XK_v);
        if (keycode != 0)
        {
            return keycode;
        }

        logger.Warning("The keyboard layout has no 'v' key; falling back to the physical V key");
        return 55;
    }

    private void HandleEvent(IntPtr eventBuffer)
    {
        var type = Marshal.ReadInt32(eventBuffer);
        if (type == X11Native.MappingNotify)
        {
            OnKeyboardMappingChanged(eventBuffer);
            return;
        }

        if (xfixesEventBase >= 0 && type == xfixesEventBase + X11Native.XFixesSelectionNotify)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        // Grabbed key presses and releases are intentionally swallowed here; SharpHook reports them.
    }

    private void OnKeyboardMappingChanged(IntPtr mappingEvent)
    {
        X11Native.XRefreshKeyboardMapping(mappingEvent);
        var keycode = ResolveTriggerKeycode();
        if (keycode == triggerKeycode)
        {
            return;
        }

        logger.Information("Keyboard layout changed; V is now keycode {Keycode}", keycode);
        if (triggerGrabbed)
        {
            UngrabKey(triggerKeycode);
            GrabKey(keycode);
        }

        if (navigationGrabbed)
        {
            // Navigation keysyms may have moved too.
            navigationGrabbed = false;
            _ = SetNavigationGrabbedAsync(true);
        }

        triggerKeycode = keycode;
    }

    private void GrabKey(int keycode)
    {
        if (keycode <= 0)
        {
            return;
        }

        foreach (var variant in LockVariants)
        {
            lastOwnErrorCode = 0;
            X11Native.XGrabKey(display, keycode, X11Native.ControlMask | variant, root, false, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        }

        X11Native.XSync(display, false);
        if (lastOwnErrorCode != 0)
        {
            logger.Warning("Grabbing keycode {Keycode} failed with X error {Error}; another application may own this shortcut", keycode, lastOwnErrorCode);
        }
    }

    private void UngrabKey(int keycode)
    {
        if (keycode <= 0)
        {
            return;
        }

        foreach (var variant in LockVariants)
        {
            X11Native.XUngrabKey(display, keycode, X11Native.ControlMask | variant, root);
        }
    }

    private static void InstallErrorHandler(IntPtr connection)
    {
        ownDisplay = connection;
        var handler = Marshal.GetFunctionPointerForDelegate(ErrorHandlerDelegate);
        var previous = X11Native.XSetErrorHandler(handler);
        if (previous != handler)
        {
            previousErrorHandler = previous;
        }
    }

    private static int OnXError(IntPtr errorDisplay, X11Native.XErrorEvent* error)
    {
        if (errorDisplay == ownDisplay)
        {
            lastOwnErrorCode = error->ErrorCode;
            return 0;
        }

        if (previousErrorHandler != IntPtr.Zero)
        {
            var previous = (delegate* unmanaged[Cdecl]<IntPtr, X11Native.XErrorEvent*, int>)previousErrorHandler;
            return previous(errorDisplay, error);
        }

        return 0;
    }
}
