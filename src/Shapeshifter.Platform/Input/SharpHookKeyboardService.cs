using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Linux;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using SharpHook.Simulation;

namespace Shapeshifter.Platform.Input;

/// <summary>
/// Global keyboard hook and paste simulator built on SharpHook (libuiohook).
/// </summary>
/// <remarks>
/// <para>
/// On Windows and macOS, keys are suppressed directly in the hook callback. libuiohook cannot suppress on
/// Linux, so there the X11 integration grabs Ctrl+V (and, during a gesture, the navigation keys) instead.
/// </para>
/// <para>
/// Our own simulated keystrokes come back through the hook. They are recognised by matching the exact
/// sequence that was sent (XRecord does not flag simulated events) and are not handed to <see cref="KeyHandler"/>.
/// </para>
/// </remarks>
public sealed class SharpHookKeyboardService : IKeyboardHook, IPasteSimulator
{
    private static readonly TimeSpan EchoTimeout = TimeSpan.FromMilliseconds(750);

    private readonly ILogger logger;
    private readonly KeyMap keyMap;
    private readonly X11Integration? x11;
    private readonly SemaphoreSlim pasteLock = new(1, 1);
    private readonly Lock echoGate = new();
    private readonly Queue<(KeyCode Key, bool IsDown)> expectedEcho = new();
    private readonly bool canSuppress;
    private SimpleGlobalHook? hook;
    private EventSimulator? simulator;
    private Task? hookTask;
    private long echoDeadline;
    private TaskCompletionSource? echoDrained;
    private TaskCompletionSource triggerReleased = CreateReleasedSignal(released: true);

    internal SharpHookKeyboardService(ILogger logger, KeyMap keyMap, X11Integration? x11)
    {
        this.logger = logger.ForContext<SharpHookKeyboardService>();
        this.keyMap = keyMap;
        this.x11 = x11;
        canSuppress = x11 is null;
    }

    public Func<KeyInput, bool>? KeyHandler { get; set; }

    public string ModifierName => keyMap.ModifierName;

    /// <summary>Delay between individual simulated key events. Some applications drop events sent back-to-back.</summary>
    public TimeSpan KeystrokeDelay { get; set; } = TimeSpan.FromMilliseconds(8);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (hook is not null)
        {
            return;
        }

        if (OperatingSystem.IsMacOS() && !UioHookProvider.Instance.IsAxApiEnabled(true))
        {
            // libuiohook cannot create the event tap without Accessibility access (UIOHOOK_ERROR_AXAPI_DISABLED),
            // so RunAsync would fail with an opaque HookException. Fail with an actionable message instead;
            // the call above has already shown the system prompt that leads to the setting.
            throw new InvalidOperationException(
                "Shapeshifter needs Accessibility access to see and send the Cmd+V keystroke. " +
                "Allow it in System Settings > Privacy & Security > Accessibility, then start Shapeshifter again.");
        }

        simulator = EventSimulator.Create("Shapeshifter");
        hook = new SimpleGlobalHook();
        hook.KeyPressed += (_, e) => OnKey(e, isDown: true);
        hook.KeyReleased += (_, e) => OnKey(e, isDown: false);

        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hook.HookEnabled += (_, _) => ready.TrySetResult();
        hookTask = hook.RunAsync(GlobalHookType.Keyboard, useBackgroundThread: true);
        _ = hookTask.ContinueWith(
            task =>
            {
                ready.TrySetException(task.Exception?.InnerException ?? new InvalidOperationException("The keyboard hook stopped."));
                if (task.IsFaulted)
                {
                    logger.Error(task.Exception, "The global keyboard hook failed");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion,
            TaskScheduler.Default);

        var completed = await Task.WhenAny(ready.Task, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)).ConfigureAwait(false);
        if (completed != ready.Task)
        {
            throw new TimeoutException("The global keyboard hook did not start within 10 seconds.");
        }

        await ready.Task.ConfigureAwait(false);

        if (x11 is not null)
        {
            await x11.SetTriggerGrabbedAsync(true).ConfigureAwait(false);
        }

        logger.Information("Keyboard hook running (suppression: {Mode})", canSuppress ? "native" : "X11 key grabs");
    }

    public void SetNavigationCaptureEnabled(bool enabled)
    {
        if (x11 is not null)
        {
            _ = x11.SetNavigationGrabbedAsync(enabled);
        }
    }

    public async Task SimulatePasteAsync(CancellationToken cancellationToken = default)
    {
        var events = simulator ?? throw new InvalidOperationException("The keyboard service has not been started.");
        await pasteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (x11 is not null)
            {
                // While V is physically held (Ctrl released first, a "rolling" release), the passive Ctrl+V grab is an
                // active keyboard grab and would capture the synthetic keystroke. It ends when V comes up, so wait for
                // that briefly.
                await Task.WhenAny(Volatile.Read(ref triggerReleased).Task, Task.Delay(TriggerReleaseTimeout, cancellationToken)).ConfigureAwait(false);

                // Our own grab would otherwise swallow the synthetic Ctrl+V.
                await x11.SetTriggerGrabbedAsync(false).ConfigureAwait(false);
            }

            var sequence = new (KeyCode Key, bool IsDown)[]
            {
                (keyMap.PasteModifier, true),
                (KeyCode.VcV, true),
                (KeyCode.VcV, false),
                (keyMap.PasteModifier, false),
            };

            TaskCompletionSource drained;
            lock (echoGate)
            {
                expectedEcho.Clear();
                foreach (var entry in sequence)
                {
                    expectedEcho.Enqueue(entry);
                }

                echoDeadline = Environment.TickCount64 + (long)EchoTimeout.TotalMilliseconds;
                echoDrained = drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            foreach (var (key, isDown) in sequence)
            {
                var result = isDown ? events.SimulateKeyPress(key) : events.SimulateKeyRelease(key);
                if (result != UioHookResult.Success)
                {
                    logger.Warning("Simulating {Key} {Direction} failed: {Result}", key, isDown ? "down" : "up", result);
                }

                await Task.Delay(KeystrokeDelay, cancellationToken).ConfigureAwait(false);
            }

            // Wait until the hook has seen our keystrokes before re-arming the grab, so they are not re-captured.
            await Task.WhenAny(drained.Task, Task.Delay(EchoTimeout, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            lock (echoGate)
            {
                expectedEcho.Clear();
                echoDrained?.TrySetResult();
                echoDrained = null;
            }

            if (x11 is not null)
            {
                await x11.SetTriggerGrabbedAsync(true).ConfigureAwait(false);
            }

            pasteLock.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            hook?.Dispose();
        }
        catch (HookException exception)
        {
            logger.Debug(exception, "Stopping the keyboard hook failed");
        }

        simulator?.Dispose();
        pasteLock.Dispose();
    }

    private void OnKey(KeyboardHookEventArgs e, bool isDown)
    {
        var keyCode = e.Data.KeyCode;
        if (IsOwnEcho(keyCode, isDown))
        {
            return;
        }

        if (keyCode == KeyCode.VcV)
        {
            TrackTrigger(isDown);
        }

        var handler = KeyHandler;
        if (handler is null)
        {
            return;
        }

        bool suppress;
        try
        {
            suppress = handler(new KeyInput(keyMap.Map(keyCode), isDown));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "The key handler failed");
            return;
        }

        if (suppress && canSuppress)
        {
            e.SuppressEvent = true;
        }
    }

    private static readonly TimeSpan TriggerReleaseTimeout = TimeSpan.FromMilliseconds(500);

    private static TaskCompletionSource CreateReleasedSignal(bool released)
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (released)
        {
            signal.SetResult();
        }

        return signal;
    }

    /// <summary>Tracks whether the user is physically holding V (our own synthetic V is filtered out before this).</summary>
    private void TrackTrigger(bool isDown)
    {
        var current = Volatile.Read(ref triggerReleased);
        if (isDown && current.Task.IsCompleted)
        {
            Volatile.Write(ref triggerReleased, CreateReleasedSignal(released: false));
        }
        else if (!isDown)
        {
            current.TrySetResult();
        }
    }

    private bool IsOwnEcho(KeyCode keyCode, bool isDown)
    {
        lock (echoGate)
        {
            if (expectedEcho.Count == 0)
            {
                return false;
            }

            if (Environment.TickCount64 > echoDeadline)
            {
                expectedEcho.Clear();
                echoDrained?.TrySetResult();
                return false;
            }

            var (expectedKey, expectedDown) = expectedEcho.Peek();
            if (!keyMap.IsSameKey(expectedKey, keyCode) || expectedDown != isDown)
            {
                return false;
            }

            expectedEcho.Dequeue();
            if (expectedEcho.Count == 0)
            {
                echoDrained?.TrySetResult();
            }

            return true;
        }
    }
}
