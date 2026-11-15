using NSubstitute;
using Serilog;
using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Tests.Clipboard;

public sealed class PollingClipboardChangeSourceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Serves a scripted sequence of tokens (repeating the last one) and counts calls.</summary>
    private sealed class TokenScript(params string[] tokens)
    {
        private readonly Lock gate = new();
        private readonly List<(int Count, TaskCompletionSource Tcs)> waiters = [];
        private int calls;

        public Func<int, Exception?> Fail { get; set; } = _ => null;

        public int Calls
        {
            get
            {
                lock (gate)
                {
                    return calls;
                }
            }
        }

        public Task<string> NextAsync(CancellationToken cancellationToken)
        {
            int call;
            lock (gate)
            {
                call = calls++;
                foreach (var waiter in waiters.Where(w => w.Count <= calls))
                {
                    waiter.Tcs.TrySetResult();
                }
            }

            if (Fail(call) is { } exception)
            {
                return Task.FromException<string>(exception);
            }

            return Task.FromResult(tokens[Math.Min(call, tokens.Length - 1)]);
        }

        public Task WaitForCallsAsync(int count)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (calls >= count)
                {
                    return Task.CompletedTask;
                }

                waiters.Add((count, tcs));
            }

            return tcs.Task.WaitAsync(Timeout, Ct);
        }
    }

    private static (PollingClipboardChangeSource Source, Func<int> Raised) Create(TokenScript script, ILogger? logger = null)
    {
        var source = new PollingClipboardChangeSource(script.NextAsync, Interval, logger ?? Substitute.For<ILogger>());
        var raised = 0;
        source.Changed += (_, _) => Interlocked.Increment(ref raised);
        return (source, () => Volatile.Read(ref raised));
    }

    [Fact]
    public async Task FirstRead_DoesNotRaise()
    {
        var script = new TokenScript("a");
        var (source, raised) = Create(script);
        using (source)
        {
            source.Start();
            await script.WaitForCallsAsync(5);
        }

        Assert.Equal(0, raised());
    }

    [Fact]
    public async Task RaisesOncePerTokenChange()
    {
        var script = new TokenScript("a", "a", "b", "b", "b", "c", "c", "a");
        var (source, raised) = Create(script);
        using (source)
        {
            source.Start();
            await script.WaitForCallsAsync(12);
        }

        // a->b, b->c, c->a
        Assert.Equal(3, raised());
    }

    [Fact]
    public async Task ChangedSenderIsTheSource()
    {
        var script = new TokenScript("a", "b");
        using var source = new PollingClipboardChangeSource(script.NextAsync, Interval, Substitute.For<ILogger>());
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.Changed += (sender, e) =>
        {
            Assert.Same(EventArgs.Empty, e);
            tcs.TrySetResult(sender);
        };

        source.Start();

        Assert.Same(source, await tcs.Task.WaitAsync(Timeout, Ct));
    }

    [Fact]
    public async Task NotStarted_DoesNotPoll()
    {
        var script = new TokenScript("a", "b");
        using (Create(script).Source)
        {
            await Task.Delay(50, Ct);
            Assert.Equal(0, script.Calls);
        }
    }

    [Fact]
    public async Task Start_Twice_UsesOneLoop()
    {
        // Each token differs from the previous one, so every poll after the first raises exactly once.
        var tokens = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray();
        var script = new TokenScript(tokens);
        var (source, raised) = Create(script);
        source.Start();
        source.Start();
        await script.WaitForCallsAsync(10);
        source.Dispose();

        Assert.Equal(script.Calls - 1, raised());
    }

    [Fact]
    public async Task ProviderExceptions_AreSurvivedAndLogged()
    {
        var logger = Substitute.For<ILogger>();
        var script = new TokenScript("a", "a", "a", "b") { Fail = call => call is 1 or 2 ? new InvalidOperationException("busy") : null };
        var (source, raised) = Create(script, logger);
        using (source)
        {
            source.Start();
            await script.WaitForCallsAsync(6);
        }

        Assert.Equal(1, raised());
        logger.Received(2).Debug(Arg.Any<InvalidOperationException>(), "Polling the clipboard change token failed");
    }

    [Fact]
    public async Task ExceptionOnFirstRead_DoesNotCountAsChange()
    {
        var script = new TokenScript("x", "a", "a") { Fail = call => call == 0 ? new InvalidOperationException() : null };
        var (source, raised) = Create(script);
        using (source)
        {
            source.Start();
            await script.WaitForCallsAsync(5);
        }

        Assert.Equal(0, raised());
    }

    [Fact]
    public async Task Dispose_StopsPolling()
    {
        var script = new TokenScript("a");
        var (source, _) = Create(script);
        source.Start();
        await script.WaitForCallsAsync(3);

        source.Dispose();
        var callsAfterDispose = script.Calls;
        await Task.Delay(100, Ct);

        Assert.Equal(callsAfterDispose, script.Calls);
    }

    [Fact]
    public async Task Dispose_CancelsTokenPassedToProvider()
    {
        var observed = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new PollingClipboardChangeSource(
            async token =>
            {
                observed.TrySetResult(token);
                await Task.Delay(System.Threading.Timeout.Infinite, token);
                return "never";
            },
            Interval,
            Substitute.For<ILogger>());
        source.Start();
        var token = await observed.Task.WaitAsync(Timeout, Ct);

        source.Dispose();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_WithoutStart_DoesNotThrow()
    {
        var source = new PollingClipboardChangeSource(_ => Task.FromResult("a"), Interval, Substitute.For<ILogger>());

        source.Dispose();
    }

    [Fact]
    public async Task Dispose_Twice_DoesNotThrow()
    {
        var script = new TokenScript("a");
        var (source, _) = Create(script);
        source.Start();
        await script.WaitForCallsAsync(1);

        source.Dispose();
        var exception = Record.Exception(source.Dispose);

        Assert.Null(exception);
    }
}
