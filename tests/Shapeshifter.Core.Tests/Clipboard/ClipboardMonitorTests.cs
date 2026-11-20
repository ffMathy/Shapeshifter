using NSubstitute;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class ClipboardMonitorTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly FakeClipboardBackend backend = new();
    private readonly FakeChangeSource changeSource = new();
    private readonly ClipboardHistory history = new();
    private readonly ILogger logger = Substitute.For<ILogger>();
    private readonly ClipboardMonitor monitor;
    private readonly List<ClipboardCapturedEventArgs> captured = [];

    public ClipboardMonitorTests()
    {
        monitor = new ClipboardMonitor(backend, changeSource, history, logger)
        {
            Debounce = TimeSpan.FromMilliseconds(30),
            SelfWriteGracePeriod = TimeSpan.Zero,
        };
        monitor.Captured += (_, e) =>
        {
            lock (captured)
            {
                captured.Add(e);
            }
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => monitor.Dispose();

    private static ClipboardSnapshot Text(string text) => new() { Text = text };

    private Task<ClipboardCapturedEventArgs> NextCaptureAsync()
    {
        var tcs = new TaskCompletionSource<ClipboardCapturedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<ClipboardCapturedEventArgs>? handler = null;
        handler = (_, e) =>
        {
            monitor.Captured -= handler;
            tcs.TrySetResult(e);
        };
        monitor.Captured += handler;
        return tcs.Task.WaitAsync(Timeout, Ct);
    }

    /// <summary>Raises a change and waits until the resulting (debounced) read has completed.</summary>
    private async Task RaiseAndWaitForReadAsync()
    {
        var reads = backend.ReadCount;
        changeSource.Raise();
        await backend.WaitForReadsAsync(reads + 1, Ct);

        // The remainder of the capture after the read is synchronous; give it a moment to finish.
        await Task.Delay(50, Ct);
    }

    [Fact]
    public void Start_SubscribesAndStartsSourceOnce()
    {
        monitor.Start();
        monitor.Start();

        Assert.Equal(1, changeSource.StartCount);
        Assert.Equal(1, changeSource.SubscriberCount);
    }

    [Fact]
    public async Task Change_RecordsTheSourceApplication()
    {
        var source = new ClipboardSource("Editor", "notes.txt - Editor", [1, 2, 3]);
        var locator = Substitute.For<IClipboardSourceLocator>();
        locator.GetCurrentSource().Returns(source);
        using var withSource = new ClipboardMonitor(backend, changeSource, history, logger, locator) { Debounce = TimeSpan.FromMilliseconds(30) };
        var tcs = new TaskCompletionSource<ClipboardItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        withSource.Captured += (_, e) => tcs.TrySetResult(e.Item);
        withSource.Start();
        backend.Current = Text("hello");

        changeSource.Raise();
        var item = await tcs.Task.WaitAsync(Timeout, Ct);

        Assert.Same(source, item.Source);
        Assert.Equal("notes.txt - Editor", item.Source!.DisplayText);
    }

    [Fact]
    public async Task Change_WhenTheSourceLocatorThrows_StillCaptures()
    {
        var locator = Substitute.For<IClipboardSourceLocator>();
        locator.GetCurrentSource().Returns(_ => throw new InvalidOperationException("boom"));
        using var withSource = new ClipboardMonitor(backend, changeSource, history, logger, locator) { Debounce = TimeSpan.FromMilliseconds(30) };
        var tcs = new TaskCompletionSource<ClipboardItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        withSource.Captured += (_, e) => tcs.TrySetResult(e.Item);
        withSource.Start();
        backend.Current = Text("hello");

        changeSource.Raise();
        var item = await tcs.Task.WaitAsync(Timeout, Ct);

        Assert.Null(item.Source);
    }

    [Fact]
    public async Task ChangeBeforeStart_IsIgnored()
    {
        backend.Current = Text("a");

        changeSource.Raise();
        await Task.Delay(100, Ct);

        Assert.Equal(0, backend.ReadCount);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task Change_CapturesIntoHistoryAndRaisesCapturedAsNew()
    {
        monitor.Start();
        backend.Current = Text("hello");
        var next = NextCaptureAsync();

        changeSource.Raise();
        var e = await next;

        Assert.True(e.IsNew);
        Assert.Equal("hello", e.Item.Snapshot.Text);
        Assert.Same(e.Item, Assert.Single(history.Items));
    }

    [Fact]
    public async Task BurstOfChanges_IsDebouncedIntoOneCapture()
    {
        monitor.Debounce = TimeSpan.FromMilliseconds(80);
        monitor.Start();
        backend.Current = Text("first");
        var next = NextCaptureAsync();

        for (var i = 0; i < 10; i++)
        {
            changeSource.Raise();
        }

        backend.Current = Text("final");
        changeSource.Raise();
        var e = await next;
        await Task.Delay(150, Ct);

        Assert.Equal("final", e.Item.Snapshot.Text);
        Assert.Equal(1, backend.ReadCount);
        Assert.Single(captured);
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task SeparatedChanges_AreCapturedSeparately()
    {
        monitor.Start();

        backend.Current = Text("a");
        var first = NextCaptureAsync();
        changeSource.Raise();
        await first;

        backend.Current = Text("b");
        var second = NextCaptureAsync();
        changeSource.Raise();
        await second;

        Assert.Equal(["b", "a"], history.Items.Select(i => i.Snapshot.Text));
    }

    [Fact]
    public async Task EmptySnapshot_IsIgnored()
    {
        monitor.Start();
        backend.Current = ClipboardSnapshot.Empty;

        await RaiseAndWaitForReadAsync();

        Assert.Equal(0, history.Count);
        Assert.Empty(captured);
    }

    [Fact]
    public async Task CaptureNowAsync_Empty_ReturnsNull()
    {
        backend.Current = ClipboardSnapshot.Empty;

        Assert.Null(await monitor.CaptureNowAsync(Ct));
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task CaptureNowAsync_CapturesWithoutStart()
    {
        backend.Current = Text("startup");

        var item = await monitor.CaptureNowAsync(Ct);

        Assert.NotNull(item);
        Assert.Equal("startup", item.Snapshot.Text);
        Assert.Same(item, Assert.Single(history.Items));
        var e = Assert.Single(captured);
        Assert.Same(item, e.Item);
        Assert.True(e.IsNew);
    }

    [Fact]
    public async Task CaptureNowAsync_ExistingContent_RaisesCapturedNotNew()
    {
        backend.Current = Text("a");
        var first = await monitor.CaptureNowAsync(Ct);
        backend.Current = Text("b");
        await monitor.CaptureNowAsync(Ct);

        backend.Current = Text("a");
        var again = await monitor.CaptureNowAsync(Ct);

        Assert.Same(first, again);
        Assert.Equal(3, captured.Count);
        Assert.False(captured[2].IsNew);
        Assert.Equal(["a", "b"], history.Items.Select(i => i.Snapshot.Text));
    }

    [Fact]
    public async Task CaptureNowAsync_IsSerialized()
    {
        backend.Current = Text("a");
        backend.ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = monitor.CaptureNowAsync(Ct);
        var second = monitor.CaptureNowAsync(Ct);
        await backend.WaitForReadsAsync(1, Ct);
        await Task.Delay(50, Ct);

        Assert.Equal(1, backend.ReadCount);
        Assert.False(second.IsCompleted);

        backend.ReadGate.SetResult();
        await Task.WhenAll(first, second).WaitAsync(Timeout, Ct);

        Assert.Equal(2, backend.ReadCount);
        Assert.Equal(1, backend.MaxConcurrentReads);
    }

    [Fact]
    public async Task BackendException_IsSwallowedAndLogged()
    {
        var failure = new InvalidOperationException("clipboard locked");
        backend.ReadException = failure;

        var item = await monitor.CaptureNowAsync(Ct);

        Assert.Null(item);
        Assert.Equal(0, history.Count);
        logger.Received(1).Warning(failure, "Could not read the clipboard");
    }

    [Fact]
    public async Task BackendException_DuringMonitoring_DoesNotStopLaterCaptures()
    {
        monitor.Start();
        backend.ReadException = new InvalidOperationException("boom");
        await RaiseAndWaitForReadAsync();
        logger.Received(1).Warning(Arg.Any<Exception>(), "Could not read the clipboard");

        backend.ReadException = null;
        backend.Current = Text("recovered");
        var next = NextCaptureAsync();
        changeSource.Raise();

        Assert.Equal("recovered", (await next).Item.Snapshot.Text);
    }

    [Fact]
    public async Task BackendOperationCanceled_WithoutCancellationRequested_IsLoggedAsFailure()
    {
        backend.ReadException = new OperationCanceledException();

        Assert.Null(await monitor.CaptureNowAsync(Ct));
        logger.Received(1).Warning(Arg.Any<OperationCanceledException>(), "Could not read the clipboard");
    }

    [Fact]
    public async Task CaptureNowAsync_CanceledDuringRead_ReturnsNullWithoutWarning()
    {
        backend.Current = Text("a");
        backend.ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();

        var capture = monitor.CaptureNowAsync(cts.Token);
        await backend.WaitForReadsAsync(1, Ct);
        await cts.CancelAsync();

        Assert.Null(await capture.WaitAsync(Timeout, Ct));
        Assert.Equal(0, history.Count);
        logger.DidNotReceive().Warning(Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task WriteAsync_WritesToBackend()
    {
        var snapshot = Text("pasted");

        await monitor.WriteAsync(snapshot, Ct);

        Assert.Same(snapshot, Assert.Single(backend.Written));
    }

    [Fact]
    public async Task WriteAsync_Null_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => monitor.WriteAsync(null!, Ct));

    [Fact]
    public async Task SelfWrittenContent_IsNotCaptured()
    {
        monitor.Start();
        await monitor.WriteAsync(Text("self"), Ct);

        await RaiseAndWaitForReadAsync();

        Assert.Equal(0, history.Count);
        Assert.Empty(captured);
    }

    [Fact]
    public async Task SelfWrittenContent_IsRecognisedByFingerprintEvenAfterGracePeriod()
    {
        await monitor.WriteAsync(new ClipboardSnapshot
        {
            Text = "self",
            RawFormats = [new RawClipboardFormat("x-source", [1])],
        }, Ct);
        await Task.Delay(20, Ct);

        // Same user-visible content, different volatile side format: same fingerprint.
        backend.Current = new ClipboardSnapshot { Text = "self", RawFormats = [new RawClipboardFormat("x-source", [2])] };

        Assert.Null(await monitor.CaptureNowAsync(Ct));
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task ReencodedVariant_WithinGracePeriod_IsIgnored()
    {
        monitor.SelfWriteGracePeriod = TimeSpan.FromSeconds(30);
        await monitor.WriteAsync(new ClipboardSnapshot { Image = [1, 2, 3] }, Ct);

        // The platform re-encoded the image: the bytes (and thus the fingerprint) differ.
        backend.Current = new ClipboardSnapshot { Image = [9, 9, 9, 9] };

        Assert.Null(await monitor.CaptureNowAsync(Ct));
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task ReencodedVariant_StaysIgnoredAfterGracePeriodEnds()
    {
        monitor.SelfWriteGracePeriod = TimeSpan.FromMilliseconds(100);
        await monitor.WriteAsync(new ClipboardSnapshot { Image = [1, 2, 3] }, Ct);
        backend.Current = new ClipboardSnapshot { Image = [9, 9, 9, 9] };
        Assert.Null(await monitor.CaptureNowAsync(Ct));

        await Task.Delay(150, Ct);

        // A repeated notification for the same re-encoded content is still attributed to Shapeshifter.
        Assert.Null(await monitor.CaptureNowAsync(Ct));
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task NewCopy_AfterGracePeriod_IsCaptured()
    {
        monitor.SelfWriteGracePeriod = TimeSpan.FromMilliseconds(100);
        monitor.Start();
        await monitor.WriteAsync(Text("self"), Ct);
        await RaiseAndWaitForReadAsync();
        Assert.Equal(0, history.Count);

        await Task.Delay(150, Ct);
        backend.Current = Text("user copy");
        var next = NextCaptureAsync();
        changeSource.Raise();
        var e = await next;

        Assert.True(e.IsNew);
        Assert.Equal("user copy", Assert.Single(history.Items).Snapshot.Text);
    }

    [Fact]
    public async Task AfterGenuineCopy_PreviouslySelfWrittenContentIsCapturedAgain()
    {
        await monitor.WriteAsync(Text("self"), Ct);
        await Task.Delay(20, Ct);

        backend.Current = Text("user copy");
        Assert.NotNull(await monitor.CaptureNowAsync(Ct));

        // The self-write marker is cleared, so copying the same text in another app later is captured.
        backend.Current = Text("self");
        var item = await monitor.CaptureNowAsync(Ct);

        Assert.NotNull(item);
        Assert.Equal(["self", "user copy"], history.Items.Select(i => i.Snapshot.Text));
    }

    [Fact]
    public async Task ChangeWhileWriteIsInProgress_IsIgnored()
    {
        backend.WriteGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var write = monitor.WriteAsync(Text("self"), Ct);

        // Some other content appears while the (slow) write is still running: attributed to the write.
        backend.Current = Text("reencoded");
        Assert.Null(await monitor.CaptureNowAsync(Ct));

        backend.WriteGate.SetResult();
        await write.WaitAsync(Timeout, Ct);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task FailedWrite_StillStartsGracePeriodAndPropagates()
    {
        monitor.SelfWriteGracePeriod = TimeSpan.FromSeconds(30);
        backend.WriteException = new InvalidOperationException("write failed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => monitor.WriteAsync(Text("self"), Ct));

        backend.Current = Text("anything");
        Assert.Null(await monitor.CaptureNowAsync(Ct));
    }

    [Fact]
    public async Task Dispose_CancelsPendingDebouncedCapture()
    {
        monitor.Debounce = TimeSpan.FromMilliseconds(80);
        monitor.Start();
        backend.Current = Text("a");

        changeSource.Raise();
        monitor.Dispose();
        await Task.Delay(200, Ct);

        Assert.Equal(0, backend.ReadCount);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public async Task Dispose_Unsubscribes()
    {
        monitor.Start();
        monitor.Dispose();
        backend.Current = Text("a");

        changeSource.Raise();
        await Task.Delay(100, Ct);

        Assert.Equal(0, changeSource.SubscriberCount);
        Assert.Equal(0, backend.ReadCount);
    }

    [Fact]
    public async Task ReadThatNeverFinishes_IsGivenUp_AndLaterCopiesAreStillCaptured()
    {
        monitor.ReadTimeout = TimeSpan.FromMilliseconds(200);
        monitor.Start();
        backend.Current = Text("lost");
        backend.NextReadHangs = true;
        await RaiseAndWaitForReadAsync();

        backend.Current = Text("after");
        var capture = NextCaptureAsync();
        changeSource.Raise();

        Assert.Equal("after", (await capture).Item.Snapshot.Text);
    }

    [Fact]
    public async Task SlowRead_IsNotCutShortByANewerChange()
    {
        monitor.Start();
        backend.Current = Text("slow");
        backend.UncancellableReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = backend.ReadCount;
        changeSource.Raise();
        await backend.WaitForReadsAsync(reads + 1, Ct);

        // A newer copy arrives while the first read is still under way.
        backend.Current = Text("newer");
        changeSource.Raise();
        await Task.Delay(100, Ct);
        backend.UncancellableReadGate.SetResult();
        await backend.WaitForReadsAsync(reads + 2, Ct);
        await Task.Delay(100, Ct);

        Assert.Equal(["newer", "slow"], history.Items.Select(i => i.Snapshot.Text));
    }

    [Fact]
    public async Task ReadThatNeverFinishes_TimesOut_AndReleasesTheNextCapture()
    {
        monitor.ReadTimeout = TimeSpan.FromMilliseconds(200);
        backend.Current = Text("stuck");
        backend.NextReadHangs = true;

        Assert.Null(await monitor.CaptureNowAsync(Ct).WaitAsync(Timeout, Ct));

        backend.Current = Text("next");
        var item = await monitor.CaptureNowAsync(Ct).WaitAsync(Timeout, Ct);
        Assert.Equal("next", item?.Snapshot.Text);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        monitor.Start();
        monitor.Dispose();
        monitor.Dispose();
    }

    private sealed class FakeChangeSource : IClipboardChangeSource
    {
        private EventHandler? changed;

        public event EventHandler? Changed
        {
            add
            {
                changed += value;
                SubscriberCount++;
            }

            remove
            {
                changed -= value;
                SubscriberCount--;
            }
        }

        public int StartCount { get; private set; }

        public int SubscriberCount { get; private set; }

        public void Start() => StartCount++;

        public void Raise() => changed?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
        }
    }

    private sealed class FakeClipboardBackend : IClipboardBackend
    {
        private readonly Lock gate = new();
        private readonly List<(int Count, TaskCompletionSource Tcs)> readWaiters = [];
        private int readCount;
        private int concurrentReads;

        private ClipboardSnapshot current = ClipboardSnapshot.Empty;

        public ClipboardSnapshot Current
        {
            get => Volatile.Read(ref current);
            set => Volatile.Write(ref current, value);
        }

        public Exception? ReadException { get; set; }

        public Exception? WriteException { get; set; }

        public TaskCompletionSource? ReadGate { get; set; }

        /// <summary>Holds reads until set, ignoring cancellation, as the real backend does for text, files and images.</summary>
        public TaskCompletionSource? UncancellableReadGate { get; set; }

        /// <summary>The next read never finishes and ignores cancellation, as an X11 read whose owner never answers.</summary>
        public bool NextReadHangs { get; set; }

        public TaskCompletionSource? WriteGate { get; set; }

        public List<ClipboardSnapshot> Written { get; } = [];

        public int ReadCount => Volatile.Read(ref readCount);

        public int MaxConcurrentReads { get; private set; }

        public async Task<ClipboardSnapshot> ReadAsync(CancellationToken cancellationToken = default)
        {
            var concurrent = Interlocked.Increment(ref concurrentReads);
            lock (gate)
            {
                MaxConcurrentReads = Math.Max(MaxConcurrentReads, concurrent);
            }

            try
            {
                // What the clipboard held when the read began, as a real read returns.
                var snapshot = Current;
                var count = Interlocked.Increment(ref readCount);
                lock (gate)
                {
                    foreach (var waiter in readWaiters.Where(w => w.Count <= count))
                    {
                        waiter.Tcs.TrySetResult();
                    }
                }

                if (NextReadHangs)
                {
                    NextReadHangs = false;
                    await new TaskCompletionSource().Task;
                }

                if (UncancellableReadGate is { } slow)
                {
                    await slow.Task;
                }

                if (ReadGate is { } readGate)
                {
                    await readGate.Task.WaitAsync(cancellationToken);
                }

                if (ReadException is { } exception)
                {
                    throw exception;
                }

                return snapshot;
            }
            finally
            {
                Interlocked.Decrement(ref concurrentReads);
            }
        }

        public async Task WriteAsync(ClipboardSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (WriteGate is { } writeGate)
            {
                await writeGate.Task.WaitAsync(cancellationToken);
            }

            if (WriteException is { } exception)
            {
                throw exception;
            }

            Written.Add(snapshot);
            Current = snapshot;
        }

        public Task<string> GetChangeTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Current.Fingerprint);

        public Task WaitForReadsAsync(int count, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (readCount >= count)
                {
                    return Task.CompletedTask;
                }

                readWaiters.Add((count, tcs));
            }

            return tcs.Task.WaitAsync(Timeout, cancellationToken);
        }
    }
}
