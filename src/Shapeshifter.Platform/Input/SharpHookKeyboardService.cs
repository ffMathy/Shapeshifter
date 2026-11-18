using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using Shapeshifter.Platform.Linux;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using SharpHook.Simulation;
using Shapeshifter.Core.Localization;

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
/// <para>
/// With Win+V on Windows, the Start menu would open when the Windows key comes up, because Windows never saw the
/// suppressed V. So the first time a key is suppressed while the Windows key is held, a Ctrl press is sent to "mask" it,
/// as other keyboard tools do: Windows only opens the Start menu when the Windows key was pressed on its own.
/// </para>
/// <para>
/// A quick Ctrl+V pastes as soon as V comes up, often while the user still holds Ctrl (issue #639). The paste then sends
/// only V, because the user's own Ctrl is already down: releasing it would leave Windows and X11 treating Ctrl as up
/// while the user holds it, so a following key (Down, say) would reach the application without Ctrl.
/// </para>
/// </remarks>
public sealed class SharpHookKeyboardService : IKeyboardHook, IPasteSimulator
{
    private static readonly TimeSpan EchoTimeout = TimeSpan.FromMilliseconds(750);

    private readonly ILogger logger;
    private volatile KeyMap keyMap;
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

    /// <summary>The left and right variants of the paste keystroke's modifier the user holds (bit 0 left, bit 1 right).</summary>
    private int pasteModifiersHeld;

    // Only touched on the hook thread.
    private bool modifierHeld;
    private bool startMenuMasked;

    internal SharpHookKeyboardService(ILogger logger, KeyMap keyMap, X11Integration? x11)
    {
        this.logger = logger.ForContext<SharpHookKeyboardService>();
        this.keyMap = keyMap;
        this.x11 = x11;
        canSuppress = x11 is null;
    }

    public Func<KeyInput, bool>? KeyHandler { get; set; }

    public string ModifierName => keyMap.ModifierName;

    public PasteShortcut PasteShortcut
    {
        get => keyMap.Shortcut;
        set
        {
            if (keyMap.Shortcut == value)
            {
                return;
            }

            keyMap = KeyMap.ForCurrentPlatform(value);
            logger.Information("Paste gesture modifier: {Modifier}", keyMap.ModifierName);
        }
    }

    public IReadOnlyList<string> ShortcutModifierNames => KeyMap.ShortcutModifierNames;

    public bool GestureModifierPastes => keyMap.GestureModifierPastes;

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
            throw new InvalidOperationException(Strings.PlatformAccessibility);
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
        if (simulator is null)
        {
            throw new InvalidOperationException("The keyboard service has not been started.");
        }

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

            // libuiohook on macOS sets the flags of a simulated key from the modifiers it simulated itself, ignoring the
            // ones the user holds, so Cmd is always sent there. That does no harm: every key the user presses next carries
            // the modifiers they physically hold, Cmd included.
            var pasteModifier = keyMap.PasteModifier;
            (KeyCode Key, bool IsDown)[] sequence = Volatile.Read(ref pasteModifiersHeld) != 0 && !OperatingSystem.IsMacOS()
                ? [(KeyCode.VcV, true), (KeyCode.VcV, false)]
                : [(pasteModifier, true), (KeyCode.VcV, true), (KeyCode.VcV, false), (pasteModifier, false)];
            await SimulateSequenceAsync(sequence, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (x11 is not null)
            {
                await x11.SetTriggerGrabbedAsync(true).ConfigureAwait(false);
            }

            pasteLock.Release();
        }
    }

    /// <summary>
    /// Sends <paramref name="sequence"/> and waits until the hook has seen it come back, so it is not handed to
    /// <see cref="KeyHandler"/>. The caller holds <see cref="pasteLock"/>, as only one sequence can be expected at a time.
    /// </summary>
    private async Task SimulateSequenceAsync((KeyCode Key, bool IsDown)[] sequence, CancellationToken cancellationToken)
    {
        var events = simulator ?? throw new InvalidOperationException("The keyboard service has not been started.");
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

        try
        {
            foreach (var (key, isDown) in sequence)
            {
                var result = isDown ? events.SimulateKeyPress(key) : events.SimulateKeyRelease(key);
                if (result != UioHookResult.Success)
                {
                    logger.Warning("Simulating {Key} {Direction} failed: {Result}", key, isDown ? "down" : "up", result);
                }

                await Task.Delay(KeystrokeDelay, cancellationToken).ConfigureAwait(false);
            }

            // Wait until the hook has seen our keystrokes (before a grab is re-armed, so they are not re-captured).
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
        }
    }

    /// <summary>Sends a Ctrl press while the Windows key is held, so releasing it does not open the Start menu.</summary>
    private async Task MaskStartMenuAsync()
    {
        try
        {
            await pasteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SimulateSequenceAsync([(KeyCode.VcLeftControl, true), (KeyCode.VcLeftControl, false)], CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally
            {
                pasteLock.Release();
            }
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            logger.Debug(exception, "Could not keep the Start menu from opening");
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

        var map = keyMap;
        if (map.IsPasteModifier(keyCode))
        {
            TrackPasteModifier(keyCode, isDown);
        }

        var key = map.Map(keyCode);
        if (key == GestureKey.Modifier)
        {
            modifierHeld = isDown;
            if (!isDown)
            {
                startMenuMasked = false;
            }
        }

        var handler = KeyHandler;
        if (handler is null)
        {
            return;
        }

        bool suppress;
        try
        {
            suppress = handler(new KeyInput(key, isDown));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "The key handler failed");
            return;
        }

        if (suppress && canSuppress)
        {
            e.SuppressEvent = true;
            if (map.UsesWindowsKey && isDown && modifierHeld && !startMenuMasked)
            {
                startMenuMasked = true;
                _ = MaskStartMenuAsync();
            }
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

    /// <summary>Tracks whether the user physically holds the paste modifier (our own synthetic one is filtered out before this).</summary>
    private void TrackPasteModifier(KeyCode keyCode, bool isDown)
    {
        var bit = keyCode is KeyCode.VcRightControl or KeyCode.VcRightMeta ? 2 : 1;
        var held = Volatile.Read(ref pasteModifiersHeld);
        Volatile.Write(ref pasteModifiersHeld, isDown ? held | bit : held & ~bit);
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
            if (!KeyMap.IsSameKey(expectedKey, keyCode) || expectedDown != isDown)
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
