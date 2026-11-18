using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Shapeshifter.Desktop.Tests;

/// <summary>
/// The real application on Avalonia's headless platform, set up once on a thread of its own that becomes its UI thread.
/// Tests hand their work to that thread with <see cref="RunAsync"/>.
/// </summary>
internal static class TestApp
{
    private static readonly BlockingCollection<Action> Work = [];

    static TestApp()
    {
        var thread = new Thread(() =>
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
            foreach (var work in Work.GetConsumingEnumerable())
            {
                work();
                Dispatcher.UIThread.RunJobs();
            }
        })
        {
            IsBackground = true,
            Name = "Avalonia UI thread",
        };
        thread.Start();
    }

    public static Task RunAsync(Action test)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Work.Add(() =>
        {
            try
            {
                test();
                done.SetResult();
            }
            catch (Exception exception)
            {
                done.SetException(exception);
            }
        });
        return done.Task;
    }
}
