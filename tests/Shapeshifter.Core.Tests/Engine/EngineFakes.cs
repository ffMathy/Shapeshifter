using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Engine;

/// <summary>A shared, ordered log of the calls the fakes receive, so tests can assert cross-component ordering.</summary>
internal sealed class CallLog
{
    private readonly List<string> entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (entries)
            {
                return entries.ToArray();
            }
        }
    }

    public void Add(string entry)
    {
        lock (entries)
        {
            entries.Add(entry);
        }
    }
}

internal sealed class FakeClipboardBackend(CallLog log) : IClipboardBackend
{
    private readonly Lock gate = new();
    private readonly List<ClipboardSnapshot> written = [];
    private ClipboardSnapshot current = ClipboardSnapshot.Empty;
    private int readCount;

    public ClipboardSnapshot Current
    {
        get
        {
            lock (gate)
            {
                return current;
            }
        }
        set
        {
            lock (gate)
            {
                current = value;
            }
        }
    }

    public IReadOnlyList<ClipboardSnapshot> Written
    {
        get
        {
            lock (gate)
            {
                return written.ToArray();
            }
        }
    }

    public int ReadCount => Volatile.Read(ref readCount);

    public Task<ClipboardSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = Current;
        Interlocked.Increment(ref readCount);
        return Task.FromResult(snapshot);
    }

    public Task WriteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            current = snapshot;
            written.Add(snapshot);
        }

        log.Add("clipboard.write:" + snapshot.Text);
        return Task.CompletedTask;
    }

    public Task<string> GetChangeTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Current.Fingerprint);
}

internal sealed class FakeChangeSource : IClipboardChangeSource
{
    private int startCount;

    public event EventHandler? Changed;

    public int StartCount => Volatile.Read(ref startCount);

    public bool IsDisposed { get; private set; }

    public void Start() => Interlocked.Increment(ref startCount);

    public void Raise() => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose() => IsDisposed = true;
}

internal sealed class FakeKeyboardHook : IKeyboardHook
{
    private readonly List<bool> navigationCaptureCalls = [];

    public Func<KeyInput, bool>? KeyHandler { get; set; }

    public string ModifierName => "Ctrl";

    public Shapeshifter.Core.Settings.PasteShortcut PasteShortcut { get; set; }

    public IReadOnlyList<string> ShortcutModifierNames => ["Ctrl"];

    public bool GestureModifierPastes { get; set; } = true;

    public int StartCount { get; private set; }

    /// <summary>Whether <see cref="KeyHandler"/> had already been assigned when <see cref="StartAsync"/> ran.</summary>
    public bool HandlerSetBeforeStart { get; private set; }

    public bool ThrowOnNavigationCapture { get; set; }

    public bool IsDisposed { get; private set; }

    public IReadOnlyList<bool> NavigationCaptureCalls
    {
        get
        {
            lock (navigationCaptureCalls)
            {
                return navigationCaptureCalls.ToArray();
            }
        }
    }

    public void SetNavigationCaptureEnabled(bool enabled)
    {
        lock (navigationCaptureCalls)
        {
            navigationCaptureCalls.Add(enabled);
        }

        if (ThrowOnNavigationCapture)
        {
            throw new InvalidOperationException("Grab failed");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCount++;
        HandlerSetBeforeStart = KeyHandler is not null;
        return Task.CompletedTask;
    }

    /// <summary>Delivers a key event the way the platform hook would.</summary>
    public bool Send(GestureKey key, bool isDown)
    {
        var handler = KeyHandler ?? throw new InvalidOperationException("No key handler is installed.");
        return handler(new KeyInput(key, isDown));
    }

    public void Dispose() => IsDisposed = true;
}

internal sealed class FakePasteSimulator(FakeClipboardBackend backend, CallLog log) : IPasteSimulator
{
    private readonly List<ClipboardSnapshot> clipboardAtPaste = [];

    public int Count
    {
        get
        {
            lock (clipboardAtPaste)
            {
                return clipboardAtPaste.Count;
            }
        }
    }

    /// <summary>What was on the (fake) clipboard each time the paste keystroke was sent.</summary>
    public IReadOnlyList<ClipboardSnapshot> ClipboardAtPaste
    {
        get
        {
            lock (clipboardAtPaste)
            {
                return clipboardAtPaste.ToArray();
            }
        }
    }

    public CancellationToken LastToken { get; private set; }

    public Task SimulatePasteAsync(CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        lock (clipboardAtPaste)
        {
            clipboardAtPaste.Add(backend.Current);
        }

        log.Add("paste");
        return Task.CompletedTask;
    }
}

internal sealed record PickerCall(string Method, PickerViewState? State);

internal sealed class FakePickerPresenter(CallLog log) : IPickerPresenter
{
    private readonly List<PickerCall> calls = [];

    public bool ThrowOnShow { get; set; }

    public IReadOnlyList<PickerCall> Calls
    {
        get
        {
            lock (calls)
            {
                return calls.ToArray();
            }
        }
    }

    public Task ShowAsync(PickerViewState state)
    {
        Record(new PickerCall("Show", state));
        return ThrowOnShow ? Task.FromException(new InvalidOperationException("Show failed")) : Task.CompletedTask;
    }

    public Task UpdateAsync(PickerViewState state)
    {
        Record(new PickerCall("Update", state));
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        Record(new PickerCall("Hide", null));
        return Task.CompletedTask;
    }

    /// <summary>What the user does in the picker while searching; set by <see cref="BeginSearchAsync"/>.</summary>
    public Func<PickerSearchInput, bool>? SearchInput { get; private set; }

    public bool ThrowOnBeginSearch { get; set; }

    public Task BeginSearchAsync(PickerViewState state, Func<PickerSearchInput, bool> onInput)
    {
        SearchInput = onInput;
        Record(new PickerCall("BeginSearch", state));
        return ThrowOnBeginSearch ? Task.FromException(new InvalidOperationException("Search failed")) : Task.CompletedTask;
    }

    /// <summary>Delivers what the user did in the focused picker, the way the window would.</summary>
    public bool Search(PickerSearchInput input) =>
        (SearchInput ?? throw new InvalidOperationException("No search has begun."))(input);

    private void Record(PickerCall call)
    {
        lock (calls)
        {
            calls.Add(call);
        }

        log.Add("picker." + call.Method);
    }
}

internal sealed class FakeFocusTracker(CallLog log) : IFocusedWindowTracker
{
    /// <summary>The window that has the focus, as <see cref="Capture"/> reports it.</summary>
    public FocusedWindow? Focused { get; set; } = new FocusedWindow(42);

    public List<FocusedWindow> Restored { get; } = [];

    public FocusedWindow? Capture()
    {
        log.Add("focus.capture");
        return Focused;
    }

    public Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default)
    {
        lock (Restored)
        {
            Restored.Add(window);
        }

        log.Add($"focus.restore:{window.Handle}");
        return Task.FromResult(true);
    }
}

internal sealed class FakeCopyNotifier : ICopyNotifier
{
    private readonly List<(ClipboardItem Item, int HistoryCount)> calls = [];

    public IReadOnlyList<(ClipboardItem Item, int HistoryCount)> Calls
    {
        get
        {
            lock (calls)
            {
                return calls.ToArray();
            }
        }
    }

    public void NotifyCopied(ClipboardItem item, int historyCount)
    {
        lock (calls)
        {
            calls.Add((item, historyCount));
        }
    }

    private readonly List<Shapeshifter.Core.Coaching.CoachingTip> tips = [];

    public IReadOnlyList<Shapeshifter.Core.Coaching.CoachingTip> Tips
    {
        get
        {
            lock (tips)
            {
                return tips.ToArray();
            }
        }
    }

    public void NotifyTip(Shapeshifter.Core.Coaching.CoachingTip tip)
    {
        lock (tips)
        {
            tips.Add(tip);
        }
    }
}

internal sealed class FakeLauncher : ILauncher
{
    public List<Uri> OpenedUrls { get; } = [];

    public List<string> OpenedFolders { get; } = [];

    public void OpenUrl(Uri uri) => OpenedUrls.Add(uri);

    public void OpenFolder(string path) => OpenedFolders.Add(path);
}

/// <summary>An action that records what it was asked to do, optionally failing.</summary>
internal sealed class RecordingAction(string id, int order, string title, CallLog log) : IClipboardAction
{
    private readonly List<ClipboardItem> executed = [];

    public string Id => id;

    public int Order => order;

    public bool Throw { get; set; }

    public IReadOnlyList<ClipboardItem> Executed
    {
        get
        {
            lock (executed)
            {
                return executed.ToArray();
            }
        }
    }

    public string GetTitle(ClipboardItem item) => title;

    public string GetDescription(ClipboardItem item) => title;

    public bool CanExecute(ClipboardItem item) => true;

    public Task ExecuteAsync(ClipboardItem item, ActionContext context, CancellationToken cancellationToken = default)
    {
        lock (executed)
        {
            executed.Add(item);
        }

        log.Add("action:" + id);
        return Throw ? Task.FromException(new InvalidOperationException("Action failed")) : Task.CompletedTask;
    }
}
