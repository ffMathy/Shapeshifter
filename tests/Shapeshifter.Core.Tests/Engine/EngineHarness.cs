using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Serilog;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Coaching;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Persistence;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Tests.Engine;

/// <summary>
/// Wires a real <see cref="ShapeshifterEngine"/> to a real history, monitor and pinned store, with fakes for
/// everything that touches the operating system. Time is a <see cref="FakeTimeProvider"/>, so the picker's
/// show delay only elapses when a test advances it.
/// </summary>
internal sealed class EngineHarness : IAsyncDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly List<GestureEffect> processed = [];
    private readonly SemaphoreSlim processedSignal = new(0);
    private bool disposed;
    private bool engineDisposed;

    public EngineHarness(Func<CallLog, ActionCatalog>? catalog = null)
    {
        Root = Path.Combine(Path.GetTempPath(), "shapeshifter-engine-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Paths = AppPaths.ForRoot(Root);
        Backend = new FakeClipboardBackend(Log);
        Monitor = new ClipboardMonitor(Backend, ChangeSource, History, Logger, SourceLocator) { Debounce = TimeSpan.FromMilliseconds(5) };
        Coach = new CopyCoach(SourceLocator, "Ctrl", time: Time);
        Paste = new FakePasteSimulator(Backend, Log);
        Picker = new FakePickerPresenter(Log);
        PinnedStore = new PinnedItemStore(Paths.PinnedDirectory, Logger);
        Catalog = catalog?.Invoke(Log) ?? ActionCatalog.CreateDefault();
        Context = new ActionContext
        {
            Clipboard = Monitor,
            Paste = Paste,
            History = History,
            PinnedItems = PinnedStore,
            Launcher = Launcher,
            Paths = Paths,
            Http = Http,
            Logger = Logger,
        };

        FocusTracker = new FakeFocusTracker(Log);

        // Most tests tap V and keep the modifier held, as with the alternative shortcuts, where releasing V does not
        // paste. The tests of the standard shortcut's paste on releasing V (issue #639) turn this back on.
        Hook.GestureModifierPastes = false;
        Engine = new ShapeshifterEngine(History, Monitor, Hook, Catalog, Context, Picker, Notifier, Logger, Time, Coach, FocusTracker);
        Engine.EffectProcessed += OnEffectProcessed;
    }

    public CallLog Log { get; } = new();

    public string Root { get; }

    public AppPaths Paths { get; }

    public FakeTimeProvider Time { get; } = new();

    public ILogger Logger { get; } = Substitute.For<ILogger>();

    public FakeClipboardBackend Backend { get; }

    public FakeChangeSource ChangeSource { get; } = new();

    public ClipboardHistory History { get; } = new();

    public ClipboardMonitor Monitor { get; }

    public FakeKeyboardHook Hook { get; } = new();

    public FakePasteSimulator Paste { get; }

    public FakePickerPresenter Picker { get; }

    public FakeCopyNotifier Notifier { get; } = new();

    /// <summary>Which application is in front: asked when a copy is noticed and when the user pastes.</summary>
    public IClipboardSourceLocator SourceLocator { get; } = Substitute.For<IClipboardSourceLocator>();

    public CopyCoach Coach { get; }

    /// <summary>Makes <paramref name="application"/> the application in front.</summary>
    public void Focus(string? application) =>
        SourceLocator.GetCurrentSource().Returns(application is null ? null : new ClipboardSource(application, application, null));

    /// <summary>Which window has the keyboard focus, as far as searching the history is concerned.</summary>
    public FakeFocusTracker FocusTracker { get; }

    public FakeLauncher Launcher { get; } = new();

    public PinnedItemStore PinnedStore { get; }

    public HttpClient Http { get; } = new();

    public ActionCatalog Catalog { get; }

    public ActionContext Context { get; }

    public ShapeshifterEngine Engine { get; }

    /// <summary>Every effect the engine has finished processing, in order.</summary>
    public IReadOnlyList<GestureEffect> Processed
    {
        get
        {
            lock (processed)
            {
                return processed.ToArray();
            }
        }
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static ClipboardSnapshot Text(string text) => new() { Text = text };

    /// <summary>Seeds the history with the given texts (the last one becomes the newest) and puts the newest on the clipboard.</summary>
    public IReadOnlyList<ClipboardItem> Seed(params string[] texts)
    {
        foreach (var text in texts)
        {
            History.Add(Text(text));
        }

        if (texts.Length > 0)
        {
            Backend.Current = Text(texts[^1]);
        }

        return History.Items;
    }

    public async Task<ClipboardItem> AddPinnedAsync(string text)
    {
        var item = new ClipboardItem(Text(text));
        await PinnedStore.SaveAsync(item, Ct);
        History.AddPinned(item);
        return item;
    }

    public Task StartAsync() => Engine.StartAsync(Ct);

    public bool Down(GestureKey key) => Hook.Send(key, isDown: true);

    public bool Up(GestureKey key) => Hook.Send(key, isDown: false);

    /// <summary>Taps a key (down then up) and returns the suppression result of the key-down.</summary>
    public bool Tap(GestureKey key)
    {
        var suppressed = Down(key);
        Up(key);
        return suppressed;
    }

    /// <summary>Waits until at least <paramref name="count"/> effects have been processed and returns them all.</summary>
    public async Task<IReadOnlyList<GestureEffect>> WaitForEffectsAsync(int count)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(Timeout);
        while (true)
        {
            lock (processed)
            {
                if (processed.Count >= count)
                {
                    return processed.ToArray();
                }
            }

            try
            {
                await processedSignal.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!Ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Expected {count} processed effects but saw {Processed.Count}: {string.Join(", ", Processed.Select(e => e.GetType().Name))}");
            }
        }
    }

    /// <summary>Waits for a condition that is reached asynchronously (for example a debounced clipboard capture).</summary>
    public static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out waiting until " + description);
            }

            await Task.Delay(5, Ct);
        }
    }

    /// <summary>Raises a clipboard change and waits until the monitor has read the clipboard and finished the capture.</summary>
    public async Task RaiseClipboardChangeAndWaitAsync()
    {
        var reads = Backend.ReadCount;
        ChangeSource.Raise();
        await WaitUntilAsync(() => Backend.ReadCount > reads, "the clipboard was read");

        // The rest of the capture after the read is synchronous; give it a moment to finish.
        await Task.Delay(50, Ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Engine.EffectProcessed -= OnEffectProcessed;
        if (!engineDisposed)
        {
            await Engine.DisposeAsync();
        }

        Monitor.Dispose();
        Http.Dispose();
        processedSignal.Dispose();
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Disposes only the engine (for tests of its own disposal), leaving the rest of the harness alive.</summary>
    public ValueTask DisposeEngineAsync()
    {
        engineDisposed = true;
        return Engine.DisposeAsync();
    }

    private void OnEffectProcessed(object? sender, GestureEffect effect)
    {
        lock (processed)
        {
            processed.Add(effect);
        }

        processedSignal.Release();
    }
}
