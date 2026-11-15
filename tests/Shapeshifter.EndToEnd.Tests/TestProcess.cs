using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Shapeshifter.EndToEnd.Tests;

/// <summary>A child process started with <c>dotnet &lt;assembly&gt;</c> whose output is captured.</summary>
internal sealed class TestProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly ConcurrentQueue<string> lines = new();
    private readonly StringBuilder log = new();
    private readonly SemaphoreSlim lineAvailable = new(0);

    private TestProcess(Process process)
    {
        this.process = process;
        process.OutputDataReceived += (_, e) => OnLine(e.Data);
        process.ErrorDataReceived += (_, e) => OnLine(e.Data is null ? null : "[stderr] " + e.Data, enqueue: false);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public bool HasExited => process.HasExited;

    public string Log
    {
        get
        {
            lock (log)
            {
                return log.ToString();
            }
        }
    }

    public static string GetAssemblyPath(string metadataKey)
    {
        var path = typeof(TestProcess).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == metadataKey).Value!;
        path = Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));
        return File.Exists(path) ? path : throw new FileNotFoundException("Build the solution first.", path);
    }

    public static TestProcess Start(string assemblyPath, IReadOnlyDictionary<string, string> environment)
    {
        var info = new ProcessStartInfo(GetDotnet(), $"\"{assemblyPath}\"")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(assemblyPath)!,
        };

        foreach (var (key, value) in environment)
        {
            info.Environment[key] = value;
        }

        return new TestProcess(Process.Start(info) ?? throw new InvalidOperationException("Could not start " + assemblyPath));
    }

    /// <summary>Waits for a line on stdout that satisfies <paramref name="predicate"/>.</summary>
    public async Task<string> WaitForLineAsync(Func<string, bool> predicate, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (true)
        {
            while (lines.TryDequeue(out var line))
            {
                if (predicate(line))
                {
                    return line;
                }
            }

            if (process.HasExited)
            {
                throw new InvalidOperationException($"The process exited with code {process.ExitCode}.\n{Log}");
            }

            try
            {
                await lineAvailable.WaitAsync(TimeSpan.FromMilliseconds(200), cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Timed out waiting for output.\n{Log}");
            }
        }
    }

    /// <summary>Sends one command line and returns the response line.</summary>
    public async Task<string> SendAsync(string command, TimeSpan? timeout = null)
    {
        while (lines.TryDequeue(out _))
        {
        }

        await process.StandardInput.WriteLineAsync(command);
        await process.StandardInput.FlushAsync();
        return await WaitForLineAsync(line => line == "OK" || line.StartsWith("TEXT ", StringComparison.Ordinal)
            || line.StartsWith("FORMATS ", StringComparison.Ordinal) || line.StartsWith("ERROR", StringComparison.Ordinal), timeout ?? TimeSpan.FromSeconds(10));
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
            {
                // Already gone.
            }
        }

        process.Dispose();
        lineAvailable.Dispose();
    }

    private void OnLine(string? line, bool enqueue = true)
    {
        if (line is null)
        {
            return;
        }

        lock (log)
        {
            log.AppendLine(line);
        }

        if (enqueue)
        {
            lines.Enqueue(line);
            lineAvailable.Release();
        }
    }

    private static string GetDotnet()
    {
        var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        var executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, executable)))
        {
            return Path.Combine(root, executable);
        }

        return Environment.ProcessPath is { } current && Path.GetFileNameWithoutExtension(current) == "dotnet" ? current : executable;
    }
}
