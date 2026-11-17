using System.ComponentModel;
using System.Diagnostics;
using Serilog;
using Shapeshifter.Core.Settings;
using Shapeshifter.Core.Updates;

namespace Shapeshifter.Desktop;

/// <summary>
/// Ensures one Shapeshifter per user by holding an exclusive lock on a file. A newer copy that is started while an older
/// one runs ends the older one and runs instead; an older or the same version leaves the running one alone.
/// </summary>
internal static class SingleInstance
{
    private static readonly TimeSpan WaitForLock = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Takes the lock for this process, ending an older running copy first when there is one. Returns null when a copy
    /// of the same or a newer version runs (or the older one could not be ended).
    /// </summary>
    public static IDisposable? Acquire(AppPaths paths, InstallLayout layout, ReleaseVersion version)
    {
        var instance = TryAcquire(paths.InstanceLockFile);
        if (instance is null)
        {
            var running = FindRunning(paths, layout);
            if (!RunningInstance.ShouldReplace(version, running))
            {
                return null;
            }

            Log.Information("Shapeshifter {Running} is running (process {Pid}); ending it to run this newer copy, {Version}", running!.Version, running.ProcessId, version);
            if (!Stop(running.ProcessId))
            {
                return null;
            }

            // The lock is released once the process is gone; the file system can take a moment.
            var deadline = DateTime.UtcNow + WaitForLock;
            while ((instance = TryAcquire(paths.InstanceLockFile)) is null && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(100);
            }

            if (instance is null)
            {
                return null;
            }
        }

        try
        {
            new RunningInstance(Environment.ProcessId, version, Environment.ProcessPath ?? string.Empty).Write(paths.InstanceInfoFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Warning(exception, "Could not record the running instance");
        }

        return new Lease(instance, paths.InstanceInfoFile);
    }

    private static IDisposable? TryAcquire(string lockFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockFile)!);
            var stream = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            stream.SetLength(0);
            var pid = System.Text.Encoding.ASCII.GetBytes(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            stream.Write(pid);
            stream.Flush();
            return stream;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// The running copy as it recorded itself; for versions that did not record themselves yet, the installed copy if it
    /// runs, with the version the installer recorded for it.
    /// </summary>
    private static RunningInstance? FindRunning(AppPaths paths, InstallLayout layout)
    {
        if (RunningInstance.Read(paths.InstanceInfoFile) is { } recorded && IsRunning(recorded))
        {
            return recorded;
        }

        if (SelfInstaller.ReadInstalledVersion(layout) is not { } installed)
        {
            return null;
        }

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id != Environment.ProcessId && process.MainModule?.FileName is { } file && layout.SamePath(file, layout.Executable))
                    {
                        return new RunningInstance(process.Id, installed, file);
                    }
                }
                catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // Another user's process, or one that already exited.
                }
            }
        }

        return null;
    }

    /// <summary>Whether the recorded process still runs that executable (process ids are reused).</summary>
    private static bool IsRunning(RunningInstance instance)
    {
        try
        {
            using var process = Process.GetProcessById(instance.ProcessId);
            var name = Path.GetFileNameWithoutExtension(instance.ExecutablePath);
            try
            {
                if (process.MainModule?.FileName is { } file)
                {
                    // Windows may run an updated copy under its renamed ".old-…" name.
                    return string.Equals(file, instance.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(file).StartsWith(Path.GetFileName(instance.ExecutablePath), StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
            {
            }

            return process.ProcessName.StartsWith(name, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Shapeshifter has no channel to ask a running copy to quit, so it is ended, as an update does (its history of
    /// unpinned items is saved only with "Remember history after restart"; pinned items are on disk).
    /// </summary>
    private static bool Stop(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill();
            if (process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                return true;
            }

            Log.Warning("The running copy (process {Pid}) did not exit", processId);
            return false;
        }
        catch (ArgumentException)
        {
            return true; // Already gone.
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            Log.Warning(exception, "Could not end the running copy (process {Pid})", processId);
            return false;
        }
    }

    /// <summary>Releases the lock and removes this instance's record.</summary>
    private sealed class Lease(IDisposable instanceLock, string infoFile) : IDisposable
    {
        public void Dispose()
        {
            RunningInstance.Remove(infoFile, Environment.ProcessId);
            instanceLock.Dispose();
        }
    }
}
