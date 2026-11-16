using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Shapeshifter.Core.Platform;
using Shapeshifter.Platform.Common;

namespace Shapeshifter.Platform.Linux;

/// <summary>The X11 window that is active when the clipboard changes.</summary>
/// <remarks>
/// Uses its own short-lived XCB connection per call (guarded by a lock), so it never shares an Xlib connection with
/// another thread and X errors (such as BadWindow for a window that closed a moment ago) come back as per-request
/// errors instead of going through Xlib's process-wide error handler, whose default terminates the process.
/// </remarks>
internal sealed unsafe class X11ClipboardSourceLocator(ILogger logger) : IClipboardSourceLocator
{
    private const int PreferredIconSize = 32;
    private const int MaxIconSide = 1024;
    private const uint MaxIconItems = 1 << 20; // 4 MB of 32-bit items is far more than any real _NET_WM_ICON
    private const uint MaxTextItems = 1024; // titles up to 4 KB

    private readonly ILogger logger = logger.ForContext<X11ClipboardSourceLocator>();
    private readonly Lock gate = new();
    private readonly int ownProcessId = Environment.ProcessId;

    public ClipboardSource? GetCurrentSource()
    {
        try
        {
            lock (gate)
            {
                return Locate();
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not determine the active X11 window");
            return null;
        }
    }

    private ClipboardSource? Locate()
    {
        var connection = X11Native.XcbConnect(IntPtr.Zero, out var screenNumber);
        try
        {
            if (connection == IntPtr.Zero || X11Native.XcbConnectionHasError(connection) != 0)
            {
                logger.Debug("Could not connect to the X server to find the active window");
                return null;
            }

            return Locate(connection, GetRootWindow(connection, screenNumber));
        }
        finally
        {
            // xcb_connect returns an error object rather than null on failure; it must be released all the same.
            if (connection != IntPtr.Zero)
            {
                X11Native.XcbDisconnect(connection);
            }
        }
    }

    private ClipboardSource? Locate(IntPtr connection, uint root)
    {
        if (root == 0)
        {
            return null;
        }

        // Interned with only_if_exists: an atom nobody created means nobody set the property.
        var atoms = InternAtoms(connection, "_NET_ACTIVE_WINDOW", "_NET_WM_NAME", "UTF8_STRING", "_NET_WM_PID", "_NET_WM_ICON");
        var (activeWindowAtom, netWmName, utf8String, netWmPid, netWmIcon) = (atoms[0], atoms[1], atoms[2], atoms[3], atoms[4]);
        if (activeWindowAtom == X11Native.XcbAtomNone)
        {
            // No EWMH window manager has ever run on this display, so there is no notion of an active window.
            return null;
        }

        var active = ReadProperty(connection, X11Native.XcbGetProperty(connection, 0, root, activeWindowAtom, X11Native.XcbAtomWindow, 0, 1));
        var window = active is { Format: 32, Value.Length: >= 4 } ? MemoryMarshal.Read<uint>(active.Value) : 0;
        if (window == 0)
        {
            return null;
        }

        // Pipeline every request, then collect the replies: one round trip to the X server.
        var titleCookie = netWmName != 0 ? X11Native.XcbGetProperty(connection, 0, window, netWmName, utf8String, 0, MaxTextItems) : default;
        var legacyTitleCookie = X11Native.XcbGetProperty(connection, 0, window, X11Native.XcbAtomWmName, X11Native.XcbAtomAny, 0, MaxTextItems);
        var classCookie = X11Native.XcbGetProperty(connection, 0, window, X11Native.XcbAtomWmClass, X11Native.XcbAtomString, 0, MaxTextItems);
        var pidCookie = netWmPid != 0 ? X11Native.XcbGetProperty(connection, 0, window, netWmPid, X11Native.XcbAtomCardinal, 0, 1) : default;
        var iconCookie = netWmIcon != 0 ? X11Native.XcbGetProperty(connection, 0, window, netWmIcon, X11Native.XcbAtomCardinal, 0, MaxIconItems) : default;

        var title = netWmName != 0 ? ReadProperty(connection, titleCookie) : null;
        var legacyTitle = ReadProperty(connection, legacyTitleCookie);
        var wmClass = ReadProperty(connection, classCookie);
        var pidProperty = netWmPid != 0 ? ReadProperty(connection, pidCookie) : null;
        var icon = netWmIcon != 0 ? ReadProperty(connection, iconCookie) : null;

        var processId = pidProperty is { Format: 32, Value.Length: >= 4 } ? (int)MemoryMarshal.Read<uint>(pidProperty.Value) : 0;
        if (processId == ownProcessId)
        {
            return null;
        }

        var windowTitle = DecodeText(title) ?? DecodeText(legacyTitle);
        var (instanceName, className) = ParseWmClass(wmClass);
        var (command, executable) = ReadProcess(processId);
        // WM_CLASS identifies the application best; the process is only consulted for windows that do not set it
        // (the process may be an interpreter or a wrapper, e.g. python3 for a Python application).
        var desktopEntry = className is not null || instanceName is not null
            ? DesktopEntryIndex.Find(className, instanceName)
            : DesktopEntryIndex.Find(executable, command);
        var applicationName = desktopEntry?.Name ?? className ?? command ?? executable;
        if (string.IsNullOrWhiteSpace(windowTitle) && string.IsNullOrWhiteSpace(applicationName))
        {
            return null;
        }

        var iconPng = EncodeIcon(icon);
        if (iconPng is null && !string.IsNullOrEmpty(desktopEntry?.Icon))
        {
            iconPng = FreedesktopIconTheme.FindPng([desktopEntry.Icon], PreferredIconSize);
        }

        return new ClipboardSource(applicationName, string.IsNullOrWhiteSpace(windowTitle) ? null : windowTitle, iconPng);
    }

    /// <summary>Interns all atoms in one round trip; unknown ones come back as None.</summary>
    private static uint[] InternAtoms(IntPtr connection, params string[] names)
    {
        var cookies = new X11Native.XcbCookie[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var bytes = Encoding.ASCII.GetBytes(names[i]);
            fixed (byte* pointer = bytes)
            {
                cookies[i] = X11Native.XcbInternAtom(connection, 1, (ushort)bytes.Length, pointer);
            }
        }

        return [.. cookies.Select(cookie => ReadAtom(connection, cookie))];
    }

    private static uint ReadAtom(IntPtr connection, X11Native.XcbCookie cookie)
    {
        var reply = X11Native.XcbInternAtomReply(connection, cookie, out var error);
        if (error != IntPtr.Zero)
        {
            X11Native.Free(error);
        }

        if (reply == IntPtr.Zero)
        {
            return X11Native.XcbAtomNone;
        }

        var atom = (uint)Marshal.ReadInt32(reply, 8);
        X11Native.Free(reply);
        return atom;
    }

    private static uint GetRootWindow(IntPtr connection, int screenNumber)
    {
        var setup = X11Native.XcbGetSetup(connection);
        if (setup == IntPtr.Zero)
        {
            return 0;
        }

        var iterator = X11Native.XcbSetupRootsIterator(setup);
        for (var i = 0; i < screenNumber && iterator.Rem > 0; i++)
        {
            X11Native.XcbScreenNext(&iterator);
        }

        return iterator.Rem > 0 && iterator.Data != IntPtr.Zero ? (uint)Marshal.ReadInt32(iterator.Data) : 0;
    }

    private sealed record Property(uint Type, int Format, byte[] Value);

    /// <summary>Reads a get-property reply; null when the property is missing or the request failed (e.g. BadWindow).</summary>
    private static Property? ReadProperty(IntPtr connection, X11Native.XcbCookie cookie)
    {
        var reply = X11Native.XcbGetPropertyReplyFor(connection, cookie, out var error);
        if (error != IntPtr.Zero)
        {
            X11Native.Free(error);
        }

        if (reply == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var header = (X11Native.XcbGetPropertyReply*)reply;
            if (header->Type == X11Native.XcbAtomNone)
            {
                return null;
            }

            // XCB (unlike Xlib's XGetWindowProperty) returns 32-bit items as 4 bytes each, in the client's byte order.
            var length = X11Native.XcbGetPropertyValueLength(reply);
            var value = length > 0 ? new ReadOnlySpan<byte>((void*)X11Native.XcbGetPropertyValue(reply), length).ToArray() : [];
            return new Property(header->Type, header->Format, value);
        }
        finally
        {
            X11Native.Free(reply);
        }
    }

    private static string? DecodeText(Property? property)
    {
        if (property is not { Format: 8 } || property.Value.Length == 0)
        {
            return null;
        }

        // STRING is Latin-1; UTF8_STRING and anything else (COMPOUND_TEXT is mostly ASCII) is read as UTF-8.
        var text = property.Type == X11Native.XcbAtomString
            ? Encoding.Latin1.GetString(property.Value)
            : Encoding.UTF8.GetString(property.Value);
        text = text.TrimEnd('\0').Trim();
        return text.Length > 0 ? text : null;
    }

    /// <summary>WM_CLASS is "instance\0class\0".</summary>
    private static (string? Instance, string? Class) ParseWmClass(Property? property)
    {
        if (property is not { Format: 8 } || property.Value.Length == 0)
        {
            return (null, null);
        }

        var parts = Encoding.Latin1.GetString(property.Value).Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (parts.Length > 0 ? parts[0] : null, parts.Length > 1 ? parts[1] : null);
    }

    /// <summary>The process name (/proc/pid/comm) and executable file name (/proc/pid/exe) of the window's owner.</summary>
    private (string? Command, string? Executable) ReadProcess(int processId)
    {
        if (processId <= 0)
        {
            return (null, null);
        }

        string? command = null;
        string? executable = null;
        try
        {
            command = File.ReadAllText($"/proc/{processId}/comm").Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.Debug(exception, "Could not read the name of process {ProcessId}", processId);
        }

        try
        {
            var target = new FileInfo($"/proc/{processId}/exe").LinkTarget;
            executable = target is null ? null : Path.GetFileName(target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Processes of other users (or in other namespaces) hide their executable; the command name will do.
        }

        return (string.IsNullOrEmpty(command) ? null : command, string.IsNullOrEmpty(executable) ? null : executable);
    }

    /// <summary>
    /// Picks the smallest _NET_WM_ICON image of at least 32 pixels (else the largest) and encodes it as PNG.
    /// The property is a list of (width, height, width×height ARGB pixels) as 32-bit cardinals.
    /// </summary>
    private static byte[]? EncodeIcon(Property? property)
    {
        if (property is not { Format: 32 } || property.Value.Length < 12)
        {
            return null;
        }

        var items = MemoryMarshal.Cast<byte, uint>(property.Value);
        var bestOffset = -1;
        var bestWidth = 0;
        var bestHeight = 0;
        var offset = 0;
        while (offset + 2 <= items.Length)
        {
            var width = items[offset];
            var height = items[offset + 1];
            if (width == 0 || height == 0 || width > MaxIconSide || height > MaxIconSide || (long)offset + 2 + (width * height) > items.Length)
            {
                break;
            }

            if (bestOffset < 0 || IsBetterIcon((int)width, (int)height, bestWidth, bestHeight))
            {
                bestOffset = offset + 2;
                bestWidth = (int)width;
                bestHeight = (int)height;
            }

            offset += 2 + (int)(width * height);
        }

        if (bestOffset < 0)
        {
            return null;
        }

        // An ARGB cardinal stored little-endian is exactly the bytes B, G, R, A, and the alpha is not premultiplied.
        var pixels = MemoryMarshal.AsBytes(items.Slice(bestOffset, bestWidth * bestHeight));
        if (!BitConverter.IsLittleEndian)
        {
            var swapped = new byte[pixels.Length];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                swapped[i] = pixels[i + 3];
                swapped[i + 1] = pixels[i + 2];
                swapped[i + 2] = pixels[i + 1];
                swapped[i + 3] = pixels[i];
            }

            return PngEncoder.EncodeBgra(swapped, bestWidth, bestHeight);
        }

        return PngEncoder.EncodeBgra(pixels, bestWidth, bestHeight);
    }

    private static bool IsBetterIcon(int width, int height, int bestWidth, int bestHeight)
    {
        var size = Math.Min(width, height);
        var bestSize = Math.Min(bestWidth, bestHeight);
        var bigEnough = size >= PreferredIconSize;
        var bestBigEnough = bestSize >= PreferredIconSize;
        if (bigEnough != bestBigEnough)
        {
            return bigEnough;
        }

        return bigEnough ? size < bestSize : size > bestSize;
    }
}

/// <summary>File and folder icons from the freedesktop icon theme.</summary>
internal sealed class FreedesktopFileIconProvider(ILogger logger) : IFileIconProvider
{
    private const string FolderMimeType = "inode/directory";

    private readonly ILogger logger = logger.ForContext<FreedesktopFileIconProvider>();

    public byte[]? GetIconPng(string path, int size)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || size <= 0)
            {
                return null;
            }

            var isFolder = FileSystemProbe.IsDirectory(path);
            var mimeType = isFolder ? FolderMimeType : MimeDatabase.GetMimeType(Path.GetFileName(path));
            return FreedesktopIconTheme.FindPng(MimeDatabase.GetIconNames(mimeType), size);
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not look up the icon for {Path}", path);
            return null;
        }
    }
}

/// <summary>Freedesktop base directories (XDG Base Directory specification).</summary>
internal static class XdgDirectories
{
    public static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string DataHome => FromEnvironment("XDG_DATA_HOME") ?? Path.Combine(Home, ".local", "share");

    public static string ConfigHome => FromEnvironment("XDG_CONFIG_HOME") ?? Path.Combine(Home, ".config");

    /// <summary>The user's data directory followed by the system ones, most important first.</summary>
    public static IReadOnlyList<string> DataDirectories
    {
        get
        {
            var system = FromEnvironment("XDG_DATA_DIRS") ?? "/usr/local/share:/usr/share";
            return [DataHome, .. system.Split(':', StringSplitOptions.RemoveEmptyEntries).Where(Path.IsPathRooted).Distinct(StringComparer.Ordinal)];
        }
    }

    private static string? FromEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

/// <summary>Reads the "key=value" lines of desktop-entry style files (.desktop, index.theme, settings.ini).</summary>
internal static class IniReader
{
    private const long MaxFileSize = 1 << 20;

    /// <summary>Yields (section, key, value) for each entry; unreadable or oversized files yield nothing.</summary>
    public static IEnumerable<(string Section, string Key, string Value)> Read(string path)
    {
        string[] lines;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaxFileSize)
            {
                yield break;
            }

            lines = File.ReadAllLines(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        var section = string.Empty;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim();
                continue;
            }

            var equals = line.IndexOf('=', StringComparison.Ordinal);
            if (equals > 0)
            {
                yield return (section, line[..equals].Trim(), line[(equals + 1)..].Trim());
            }
        }
    }
}

/// <summary>Maps window classes and process names to installed applications' .desktop entries.</summary>
internal static class DesktopEntryIndex
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(2);
    private static Snapshot? current;
    private static int loading;

    public sealed record DesktopEntry(string Name, string? Icon);

    private sealed record Snapshot(
        Dictionary<string, DesktopEntry> ByWindowClass,
        Dictionary<string, DesktopEntry> ByFileName,
        DateTime LoadedAt);

    /// <summary>
    /// Starts reading the installed .desktop files in the background (there are often hundreds), so that the first
    /// copy does not wait for it.
    /// </summary>
    public static void Warm() => RefreshInBackgroundIfStale();

    /// <summary>
    /// Finds the entry for the first candidate that matches a StartupWMClass or a .desktop file name. Never reads the
    /// disk: until the index has been built in the background, nothing is found.
    /// </summary>
    public static DesktopEntry? Find(params ReadOnlySpan<string?> candidates)
    {
        RefreshInBackgroundIfStale();
        var snapshot = Volatile.Read(ref current);
        if (snapshot is null)
        {
            return null;
        }

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && snapshot.ByWindowClass.TryGetValue(candidate, out var entry))
            {
                return entry;
            }
        }

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && snapshot.ByFileName.TryGetValue(candidate, out var entry))
            {
                return entry;
            }
        }

        return null;
    }

    private static void RefreshInBackgroundIfStale()
    {
        var snapshot = Volatile.Read(ref current);
        if (snapshot is not null && DateTime.UtcNow - snapshot.LoadedAt <= RefreshInterval)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref loading, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                Volatile.Write(ref current, Load());
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Try again on the next lookup after the interval.
                Volatile.Write(ref current, new Snapshot(new(StringComparer.OrdinalIgnoreCase), new(StringComparer.OrdinalIgnoreCase), DateTime.UtcNow));
            }
            finally
            {
                Volatile.Write(ref loading, 0);
            }
        });
    }

    private static Snapshot Load()
    {
        var windowClasses = new Dictionary<string, DesktopEntry>(StringComparer.OrdinalIgnoreCase);
        var fileNames = new Dictionary<string, DesktopEntry>(StringComparer.OrdinalIgnoreCase);

        // Desktop-file IDs (and window classes) already decided by an earlier data directory, including entries the
        // user deleted with Hidden=true, which must keep hiding the same entry in later directories.
        var seenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenWindowClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataDirectory in XdgDirectories.DataDirectories)
        {
            var applications = Path.Combine(dataDirectory, "applications");
            IEnumerable<string> files;
            try
            {
                if (!Directory.Exists(applications))
                {
                    continue;
                }

                files = Directory.EnumerateFiles(applications, "*.desktop", new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 3, IgnoreInaccessible = true }).ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                string? name = null;
                string? icon = null;
                string? windowClass = null;
                var hidden = false;
                foreach (var (section, key, value) in IniReader.Read(file))
                {
                    if (section != "Desktop Entry")
                    {
                        continue;
                    }

                    switch (key)
                    {
                        case "Name": name = value; break;
                        case "Icon": icon = value; break;
                        case "StartupWMClass": windowClass = value; break;
                        case "Hidden": hidden = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); break;
                        default: break;
                    }
                }

                // Earlier data directories take precedence; Hidden=true marks an entry the user deleted.
                var fileName = Path.GetFileNameWithoutExtension(file);
                var firstForFile = seenFileNames.Add(fileName);
                var firstForClass = !string.IsNullOrWhiteSpace(windowClass) && seenWindowClasses.Add(windowClass);
                if (hidden || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var entry = new DesktopEntry(name, string.IsNullOrWhiteSpace(icon) ? null : icon);
                if (firstForFile)
                {
                    fileNames[fileName] = entry;
                }

                if (firstForClass)
                {
                    windowClasses[windowClass!] = entry;
                }
            }
        }

        return new Snapshot(windowClasses, fileNames, DateTime.UtcNow);
    }
}

/// <summary>The shared-mime-info database: file name → MIME type → icon names.</summary>
internal static class MimeDatabase
{
    private static readonly Lazy<Database> Instance = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly (string Extension, string MimeType)[] BuiltIn =
    [
        (".txt", "text/plain"), (".log", "text/x-log"), (".md", "text/markdown"), (".csv", "text/csv"),
        (".html", "text/html"), (".htm", "text/html"), (".css", "text/css"), (".xml", "application/xml"),
        (".json", "application/json"), (".js", "application/javascript"), (".cs", "text/x-csharp"),
        (".py", "text/x-python3"), (".sh", "application/x-shellscript"), (".c", "text/x-csrc"), (".h", "text/x-chdr"),
        (".png", "image/png"), (".jpg", "image/jpeg"), (".jpeg", "image/jpeg"), (".gif", "image/gif"),
        (".bmp", "image/bmp"), (".svg", "image/svg+xml"), (".webp", "image/webp"), (".ico", "image/vnd.microsoft.icon"),
        (".mp3", "audio/mpeg"), (".wav", "audio/x-wav"), (".ogg", "audio/x-vorbis+ogg"), (".flac", "audio/flac"),
        (".mp4", "video/mp4"), (".mkv", "video/x-matroska"), (".webm", "video/webm"), (".avi", "video/x-msvideo"),
        (".pdf", "application/pdf"), (".zip", "application/zip"), (".tar", "application/x-tar"),
        (".gz", "application/gzip"), (".tar.gz", "application/x-compressed-tar"), (".7z", "application/x-7z-compressed"),
        (".doc", "application/msword"), (".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        (".xls", "application/vnd.ms-excel"), (".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        (".ppt", "application/vnd.ms-powerpoint"), (".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
        (".odt", "application/vnd.oasis.opendocument.text"), (".deb", "application/vnd.debian.binary-package"),
        (".exe", "application/x-ms-dos-executable"), (".iso", "application/x-cd-image"),
    ];

    private sealed record Database(
        Dictionary<string, string> BySuffix,
        Dictionary<string, string> ByName,
        Dictionary<string, string> GenericIcons);

    /// <summary>The MIME type for a file name, or null when unknown. The longest matching suffix wins (".tar.gz").</summary>
    public static string? GetMimeType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var database = Instance.Value;
        if (database.ByName.TryGetValue(fileName, out var byName))
        {
            return byName;
        }

        for (var dot = fileName.IndexOf('.', StringComparison.Ordinal); dot >= 0; dot = fileName.IndexOf('.', dot + 1))
        {
            if (database.BySuffix.TryGetValue(fileName[dot..], out var mimeType))
            {
                return mimeType;
            }
        }

        return null;
    }

    /// <summary>
    /// Icon names to try, most specific first: "text-plain", then the generic icon ("text-x-generic"), then
    /// "text-x-generic" for anything unknown. Directories ("inode/directory") are "folder".
    /// </summary>
    public static IReadOnlyList<string> GetIconNames(string? mimeType)
    {
        if (string.Equals(mimeType, "inode/directory", StringComparison.Ordinal))
        {
            return ["folder"];
        }

        var names = new List<string>(3);
        if (!string.IsNullOrEmpty(mimeType))
        {
            names.Add(mimeType.Replace('/', '-'));
            var slash = mimeType.IndexOf('/', StringComparison.Ordinal);
            var generic = Instance.Value.GenericIcons.TryGetValue(mimeType, out var listed)
                ? listed
                : slash > 0 ? mimeType[..slash] + "-x-generic" : null;
            if (generic is not null && !names.Contains(generic))
            {
                names.Add(generic);
            }
        }

        if (!names.Contains("text-x-generic"))
        {
            names.Add("text-x-generic");
        }

        return names;
    }

    private static Database Load()
    {
        var bySuffix = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Globs are case-insensitive unless flagged "cs", which is rare enough to ignore.
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var weights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var genericIcons = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var dataDirectory in XdgDirectories.DataDirectories)
        {
            var mimeDirectory = Path.Combine(dataDirectory, "mime");
            foreach (var line in ReadLines(Path.Combine(mimeDirectory, "globs2")))
            {
                ParseGlob(line, bySuffix, byName, weights);
            }

            foreach (var line in ReadLines(Path.Combine(mimeDirectory, "generic-icons")))
            {
                var colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon > 0 && colon < line.Length - 1)
                {
                    genericIcons.TryAdd(line[..colon], line[(colon + 1)..].Trim());
                }
            }
        }

        foreach (var (extension, mimeType) in BuiltIn)
        {
            bySuffix.TryAdd(extension, mimeType);
        }

        return new Database(bySuffix, byName, genericIcons);
    }

    /// <summary>Parses a globs2 line, "weight:mime/type:glob[:flags]". Only "*.suffix" and literal names are used.</summary>
    private static void ParseGlob(string line, Dictionary<string, string> bySuffix, Dictionary<string, string> byName, Dictionary<string, int> weights)
    {
        var parts = line.Split(':');
        if (parts.Length < 3 || !int.TryParse(parts[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var weight))
        {
            return;
        }

        var mimeType = parts[1];
        var glob = parts[2];
        if (mimeType.Length == 0 || glob.Length == 0)
        {
            return;
        }

        if (glob.StartsWith("*.", StringComparison.Ordinal) && glob.AsSpan(1).IndexOfAny("*?[") < 0)
        {
            var suffix = glob[1..];
            if (!weights.TryGetValue(suffix, out var existing) || weight > existing)
            {
                bySuffix[suffix] = mimeType;
                weights[suffix] = weight;
            }
        }
        else if (glob.AsSpan().IndexOfAny("*?[") < 0)
        {
            byName.TryAdd(glob, mimeType);
        }
    }

    private static IEnumerable<string> ReadLines(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllLines(path).Where(line => line.Length > 0 && line[0] != '#') : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}

/// <summary>
/// Looks icons up in the current freedesktop icon theme, its parents, Adwaita and hicolor (Icon Theme specification),
/// returning PNG files only. Results, including misses, are cached by icon names and size.
/// </summary>
internal static class FreedesktopIconTheme
{
    private const int MaxCacheEntries = 256;
    private const long MaxIconFileSize = 4 << 20;
    private const int TooSmallPenalty = 100_000;
    private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, byte[]?> Cache = new(StringComparer.Ordinal);
    private static readonly Lazy<List<Theme>> Themes = new(LoadThemeChain, LazyThreadSafetyMode.ExecutionAndPublication);

    private enum DirectoryType
    {
        Threshold,
        Fixed,
        Scalable,
    }

    private sealed record IconDirectory(string Path, int Size, int MinSize, int MaxSize, int Threshold, int Scale, DirectoryType Type)
    {
        /// <summary>The pixel size this directory offers for a request of <paramref name="requested"/> pixels.</summary>
        public int PixelSizeFor(int requested)
        {
            var nominal = Type switch
            {
                DirectoryType.Fixed => Size,
                DirectoryType.Scalable => Math.Clamp(requested / Scale, MinSize, MaxSize),
                _ => Math.Abs((requested / Scale) - Size) <= Threshold ? requested / Scale : Size,
            };
            return nominal * Scale;
        }
    }

    private sealed record Theme(string Name, IReadOnlyList<string> Roots, IReadOnlyList<IconDirectory> Directories, IReadOnlyList<string> Inherits);

    /// <summary>The first of <paramref name="iconNames"/> found as a PNG, closest to <paramref name="size"/> pixels.</summary>
    public static byte[]? FindPng(IReadOnlyList<string> iconNames, int size)
    {
        var key = string.Join('|', iconNames) + "@" + size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        // Looked up outside the lock, so a slow lookup on the clipboard thread never holds up the UI thread's.
        var result = Lookup(iconNames, size);
        lock (Gate)
        {
            if (Cache.Count >= MaxCacheEntries)
            {
                Cache.Clear();
            }

            Cache[key] = result;
        }

        return result;
    }

    private static byte[]? Lookup(IReadOnlyList<string> iconNames, int size)
    {
        foreach (var name in iconNames)
        {
            // Icon= in a .desktop file may be an absolute path.
            if (Path.IsPathRooted(name))
            {
                if (LoadPng(name) is { } absolute)
                {
                    return absolute;
                }
            }
        }

        foreach (var theme in Themes.Value)
        {
            foreach (var name in iconNames)
            {
                if (IsValidIconName(name) && FindInTheme(theme, name, size) is { } file && LoadPng(file) is { } bytes)
                {
                    return bytes;
                }
            }
        }

        foreach (var name in iconNames)
        {
            if (IsValidIconName(name) && LoadPng(Path.Combine("/usr/share/pixmaps", name + ".png")) is { } bytes)
            {
                return bytes;
            }
        }

        return null;
    }

    private static bool IsValidIconName(string name) =>
        name.Length > 0 && name.IndexOfAny(['/', '\\']) < 0 && name != "." && name != "..";

    private static string? FindInTheme(Theme theme, string name, int size)
    {
        string? best = null;
        var bestScore = int.MaxValue;
        var fileName = name + ".png";
        foreach (var directory in theme.Directories)
        {
            var pixels = directory.PixelSizeFor(size);
            var score = pixels >= size ? pixels - size : TooSmallPenalty + (size - pixels);
            if (score >= bestScore)
            {
                continue;
            }

            foreach (var root in theme.Roots)
            {
                var candidate = Path.Combine(root, directory.Path, fileName);
                if (File.Exists(candidate))
                {
                    best = candidate;
                    bestScore = score;
                    break;
                }
            }

            if (bestScore == 0)
            {
                break;
            }
        }

        return best;
    }

    private static byte[]? LoadPng(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < PngSignature.Length || info.Length > MaxIconFileSize)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            return bytes.AsSpan().StartsWith(PngSignature) ? bytes : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The user's theme and everything it inherits, then Adwaita and hicolor, each once.</summary>
    private static List<Theme> LoadThemeChain()
    {
        var baseDirectories = new List<string>
        {
            Path.Combine(XdgDirectories.DataHome, "icons"),
            Path.Combine(XdgDirectories.Home, ".icons"),
        };
        baseDirectories.AddRange(XdgDirectories.DataDirectories.Skip(1).Select(directory => Path.Combine(directory, "icons")));

        var chain = new List<Theme>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        if (ReadConfiguredThemeName() is { } configured)
        {
            pending.Enqueue(configured);
        }

        pending.Enqueue("Adwaita");
        pending.Enqueue("hicolor");
        while (pending.Count > 0 && chain.Count < 32)
        {
            var name = pending.Dequeue();
            if (!seen.Add(name) || LoadTheme(name, baseDirectories) is not { } theme)
            {
                continue;
            }

            chain.Add(theme);

            // Parents come right after their child, ahead of the fallbacks still queued.
            var rest = pending.ToList();
            pending.Clear();
            foreach (var parent in theme.Inherits.Where(parent => parent != "hicolor"))
            {
                pending.Enqueue(parent);
            }

            foreach (var queued in rest)
            {
                pending.Enqueue(queued);
            }
        }

        return chain;
    }

    private static string? ReadConfiguredThemeName()
    {
        foreach (var file in new[] { Path.Combine(XdgDirectories.ConfigHome, "gtk-3.0", "settings.ini"), Path.Combine(XdgDirectories.ConfigHome, "gtk-4.0", "settings.ini") })
        {
            foreach (var (section, key, value) in IniReader.Read(file))
            {
                if (section == "Settings" && key == "gtk-icon-theme-name" && value.Trim('"').Length > 0)
                {
                    return value.Trim('"');
                }
            }
        }

        return null;
    }

    private static Theme? LoadTheme(string name, IReadOnlyList<string> baseDirectories)
    {
        if (!IsValidIconName(name))
        {
            return null;
        }

        var roots = baseDirectories.Select(directory => Path.Combine(directory, name)).Where(Directory.Exists).ToList();
        var indexFile = roots.Select(root => Path.Combine(root, "index.theme")).FirstOrDefault(File.Exists);
        if (indexFile is null)
        {
            return null;
        }

        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (section, key, value) in IniReader.Read(indexFile))
        {
            if (!sections.TryGetValue(section, out var entries))
            {
                sections[section] = entries = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            entries.TryAdd(key, value);
        }

        if (!sections.TryGetValue("Icon Theme", out var header))
        {
            return null;
        }

        var directories = new List<IconDirectory>();
        var listed = SplitList(header.GetValueOrDefault("Directories")).Concat(SplitList(header.GetValueOrDefault("ScaledDirectories"))).Distinct(StringComparer.Ordinal);
        foreach (var path in listed)
        {
            if (!sections.TryGetValue(path, out var entries) || !TryParse(entries, "Size", out var size) || size <= 0)
            {
                continue;
            }

            var type = entries.GetValueOrDefault("Type") switch
            {
                "Fixed" => DirectoryType.Fixed,
                "Scalable" => DirectoryType.Scalable,
                _ => DirectoryType.Threshold,
            };
            var minSize = TryParse(entries, "MinSize", out var min) ? min : size;
            var maxSize = TryParse(entries, "MaxSize", out var max) ? max : size;
            var threshold = TryParse(entries, "Threshold", out var t) ? t : 2;
            var scale = TryParse(entries, "Scale", out var s) && s > 0 ? s : 1;
            directories.Add(new IconDirectory(path, size, Math.Min(minSize, maxSize), Math.Max(minSize, maxSize), threshold, scale, type));
        }

        return new Theme(name, roots, directories, SplitList(header.GetValueOrDefault("Inherits")).ToList());
    }

    private static IEnumerable<string> SplitList(string? value) =>
        string.IsNullOrEmpty(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool TryParse(Dictionary<string, string> entries, string key, out int value)
    {
        value = 0;
        return entries.TryGetValue(key, out var text) && int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
