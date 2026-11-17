using System.Diagnostics;
using System.Globalization;

namespace Shapeshifter.Core.Updates;

/// <summary>
/// Puts a new copy of the application in place of the old one, also while the old one is running. The new copy is
/// first written next to the target (<c>target.new-…</c>), so the final step is a rename on the same drive.
/// </summary>
/// <remarks>
/// Windows does not allow replacing or deleting a running executable but does allow renaming it, so the old copy is
/// renamed to <c>target.old-…</c> first and deleted on a later start (<see cref="CleanLeftovers"/>). On macOS and
/// Linux a running executable can be replaced directly: the process keeps the old file open until it exits.
/// </remarks>
public static class FileSwap
{
    private const string NewMarker = ".new-";
    private const string OldMarker = ".old-";

    /// <summary>A free path next to <paramref name="target"/> to write the new copy to.</summary>
    public static string StagingPath(string target) => Unique(target, NewMarker);

    /// <summary>Moves <paramref name="staged"/> (a file, or a folder for an app bundle) to <paramref name="target"/>.</summary>
    public static void Replace(string staged, string target)
    {
        ArgumentException.ThrowIfNullOrEmpty(staged);
        ArgumentException.ThrowIfNullOrEmpty(target);
        var isDirectory = Directory.Exists(staged);
        if (!isDirectory && !File.Exists(staged))
        {
            throw new FileNotFoundException("The new copy to put in place is missing.", staged);
        }

        if (!isDirectory && !OperatingSystem.IsWindows())
        {
            // rename(2): atomic, and the running process keeps its open copy of the old file.
            File.Move(staged, target, overwrite: true);
            return;
        }

        string? old = null;
        if (File.Exists(target) || Directory.Exists(target))
        {
            old = Unique(target, OldMarker);
            Move(target, old);
        }

        try
        {
            Move(staged, target);
        }
        catch
        {
            if (old is not null)
            {
                Move(old, target);
            }

            throw;
        }

        if (old is not null)
        {
            TryDelete(old);
        }
    }

    /// <summary>
    /// Deletes what earlier installs and updates left next to <paramref name="target"/>: old copies that were still
    /// running and new copies whose download was interrupted. Anything still in use is left for a later start.
    /// </summary>
    public static void CleanLeftovers(string target)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(target));
        if (directory is null || !Directory.Exists(directory))
        {
            return;
        }

        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(target));
        foreach (var marker in new[] { OldMarker, NewMarker })
        {
            IEnumerable<string> leftovers;
            try
            {
                leftovers = Directory.EnumerateFileSystemEntries(directory, name + marker + "*").ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return;
            }

            foreach (var leftover in leftovers)
            {
                TryDelete(leftover);
            }
        }
    }

    /// <summary>Deletes a file or a folder, ignoring failures (a running executable on Windows cannot be deleted).</summary>
    public static bool TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path) && !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path) || Directory.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Makes a copied or downloaded executable runnable (Unix only).</summary>
    public static void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    /// <summary>
    /// Forgets where a file came from, so the copy the installer made does not ask again whether to run a file from
    /// the internet: the user already chose to run it. Removes the Mark of the Web on Windows (the
    /// <c>Zone.Identifier</c> stream) and the quarantine attribute on macOS.
    /// </summary>
    public static void RemoveDownloadMark(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                File.Delete(path + ":Zone.Identifier");
            }
            else if (OperatingSystem.IsMacOS())
            {
                RunTool("/usr/bin/xattr", "-dr", "com.apple.quarantine", path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Only means the system may ask once more before the first start.
        }
    }

    /// <summary>Runs a command-line tool and throws when it fails.</summary>
    internal static void RunTool(string fileName, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} did not finish.");
        }

        Task.WaitAll(output, error);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed ({process.ExitCode}): {error.Result.Trim()}");
        }
    }

    private static void Move(string from, string to)
    {
        if (Directory.Exists(from))
        {
            Directory.Move(from, to);
        }
        else
        {
            File.Move(from, to);
        }
    }

    private static string Unique(string target, string marker)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
        while (true)
        {
            var candidate = trimmed + marker + Random.Shared.Next(0x10000, 0xFFFFF).ToString("x", CultureInfo.InvariantCulture);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
