namespace Shapeshifter.Core.Platform;

/// <summary>
/// Existence checks that are safe on the UI thread: they never touch a network location, where a slow or disconnected
/// share can block for seconds. For network paths the answer is guessed from the path alone.
/// </summary>
public static class FileSystemProbe
{
    /// <summary>
    /// Whether <paramref name="path"/> is a folder: it ends in a directory separator or is an existing local directory.
    /// A network path without an extension is assumed to be a folder.
    /// </summary>
    public static bool IsDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (Path.EndsInDirectorySeparator(path))
        {
            return true;
        }

        return IsNetworkPath(path) ? !Path.HasExtension(path) : Directory.Exists(path);
    }

    /// <summary>Whether <paramref name="path"/> is an existing local file; always <see langword="false"/> on a network.</summary>
    public static bool IsLocalFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return !IsNetworkPath(path) && File.Exists(path);
    }

    /// <summary>
    /// Whether <paramref name="path"/> is on a network: a UNC path (<c>\\server\share</c> or <c>//server/share</c>) or,
    /// on Windows, a drive letter mapped to a network share. Unknown or invalid paths count as network paths.
    /// </summary>
    public static bool IsNetworkPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var root = Path.GetPathRoot(path);
            // GetDriveType only looks at the drive letter's mapping; it does not contact the share.
            return string.IsNullOrEmpty(root) || new DriveInfo(root).DriveType is DriveType.Network or DriveType.NoRootDirectory;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}
