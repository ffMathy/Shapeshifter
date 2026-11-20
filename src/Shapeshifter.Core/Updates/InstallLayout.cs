using System.Runtime.InteropServices;

namespace Shapeshifter.Core.Updates;

/// <summary>The operating systems the installer knows how to install on.</summary>
public enum InstallPlatform
{
    Windows,
    MacOS,
    Linux,
}

/// <summary>Messages from the installer and the updater, for the application's log.</summary>
public interface IUpdateLog
{
    void Information(string message);

    void Warning(string message, Exception? exception = null);
}

/// <summary>Where the application is installed for the current user. Nothing here needs administrator rights.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>Windows: <c>%LOCALAPPDATA%\Programs\Name\Name.exe</c>, with a Start menu shortcut and an Apps &amp; features entry.</item>
/// <item>macOS: <c>~/Applications/Name.app</c>.</item>
/// <item>Linux: <c>~/.local/bin/name</c>, with a desktop entry in <c>~/.local/share/applications</c>.</item>
/// </list>
/// </remarks>
public sealed class InstallLayout
{
    private InstallLayout(InstallPlatform platform, string target, string executable, string stateFile, string dataHome)
    {
        Platform = platform;
        Target = target;
        Executable = executable;
        StateFile = stateFile;
        DataHome = dataHome;
    }

    public InstallPlatform Platform { get; }

    /// <summary>What gets copied and replaced: the executable, or the app bundle folder on macOS.</summary>
    public string Target { get; }

    /// <summary>The installed executable (inside the bundle on macOS).</summary>
    public string Executable { get; }

    public bool TargetIsBundle => Platform == InstallPlatform.MacOS;

    /// <summary>Remembers which version is installed, so an older download does not replace a newer installation.</summary>
    public string StateFile { get; }

    /// <summary>The XDG data folder that holds desktop entries and icons on Linux (<c>~/.local/share</c>).</summary>
    public string DataHome { get; }

    /// <summary>
    /// True for the real installation of the current user (<see cref="ForCurrentUser"/>); only that one gets Start
    /// menu shortcuts, Apps &amp; features entries and desktop entries.
    /// </summary>
    public bool IsForCurrentUser { get; private init; }

    public static InstallPlatform CurrentPlatform =>
        OperatingSystem.IsWindows() ? InstallPlatform.Windows : OperatingSystem.IsMacOS() ? InstallPlatform.MacOS : InstallPlatform.Linux;

    /// <summary>The layout for the current user on the current operating system.</summary>
    /// <param name="stateDirectory">A per-user folder of the application, for the installation's state file.</param>
    public static InstallLayout ForCurrentUser(AppIdentity app, string stateDirectory)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var layout = Create(
            app,
            CurrentPlatform,
            home,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            stateDirectory,
            string.IsNullOrWhiteSpace(data) || !Path.IsPathRooted(data) ? null : data);
        return new InstallLayout(layout.Platform, layout.Target, layout.Executable, layout.StateFile, layout.DataHome) { IsForCurrentUser = true };
    }

    public static InstallLayout Create(
        AppIdentity app, InstallPlatform platform, string home, string localApplicationData, string stateDirectory, string? dataHome = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        var stateFile = Path.Combine(stateDirectory, "install.json");
        dataHome ??= Path.Combine(home, ".local", "share");
        switch (platform)
        {
            case InstallPlatform.Windows:
            {
                var executable = Path.Combine(localApplicationData, "Programs", app.Name, app.Name + ".exe");
                return new InstallLayout(platform, executable, executable, stateFile, dataHome);
            }

            case InstallPlatform.MacOS:
            {
                var bundle = Path.Combine(home, "Applications", app.Name + ".app");
                return new InstallLayout(platform, bundle, Path.Combine(bundle, "Contents", "MacOS", app.Name), stateFile, dataHome);
            }

            default:
            {
                var executable = Path.Combine(home, ".local", "bin", app.LinuxExecutableName);
                return new InstallLayout(platform, executable, executable, stateFile, dataHome);
            }
        }
    }

    /// <summary>True when <paramref name="running"/> is the installed copy.</summary>
    public bool IsInstalled(RunningApp running)
    {
        ArgumentNullException.ThrowIfNull(running);
        return SamePath(running.ExecutablePath, Executable);
    }

    /// <summary>Compares paths the way the file system does (case-insensitively on Windows and macOS).</summary>
    public bool SamePath(string left, string right)
    {
        var comparison = Platform == InstallPlatform.Linux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(Normalize(left), Normalize(right), comparison);
    }

    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}

/// <summary>The executable this process runs from.</summary>
/// <param name="ExecutablePath">The native executable.</param>
/// <param name="BundlePath">The <c>.app</c> folder around it on macOS, or null.</param>
/// <param name="IsSingleFile">
/// True for a published single-file build: only those are installed and updated, so <c>dotnet run</c>, tests and
/// other builds that consist of many files stay where they are.
/// </param>
/// <param name="IsStoreInstall">
/// True when the Microsoft Store or the Mac App Store installed this copy (see <see cref="StoreInstall"/>): the store
/// updates it, so it is neither installed nor updated from GitHub.
/// </param>
public sealed record RunningApp(string ExecutablePath, string? BundlePath, bool IsSingleFile, bool IsStoreInstall = false)
{
    /// <summary>What an update replaces: the bundle on macOS, otherwise the executable.</summary>
    public string ReplaceablePath => BundlePath ?? ExecutablePath;

    public static RunningApp Current()
    {
        var executable = Environment.ProcessPath ?? string.Empty;
        var host = Path.GetFileNameWithoutExtension(executable);
        var entryName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;

        // Single-file builds carry the application's assemblies inside the executable; every other build has them
        // as files next to it (and "dotnet App.dll" runs from the dotnet host).
        var isSingleFile = executable.Length > 0
            && !string.Equals(host, "dotnet", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(entryName)
            && !File.Exists(Path.Combine(AppContext.BaseDirectory, entryName + ".dll"));
        return new RunningApp(executable, BundleOf(executable), isSingleFile, StoreInstall.IsActive);
    }

    /// <summary>The <c>X.app</c> folder when <paramref name="executable"/> is <c>X.app/Contents/MacOS/X</c>.</summary>
    internal static string? BundleOf(string executable)
    {
        const string marker = ".app/Contents/MacOS/";
        var index = executable.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || executable.IndexOf('/', index + marker.Length) >= 0)
        {
            return null;
        }

        return executable[..(index + ".app".Length)];
    }

    /// <summary>The release build for this computer, such as <c>win-arm64</c>, <c>osx-x64</c> or <c>linux-x64</c>.</summary>
    public static string RuntimeIdentifier(InstallPlatform platform, Architecture architecture)
    {
        var os = platform switch
        {
            InstallPlatform.Windows => "win",
            InstallPlatform.MacOS => "osx",
            _ => "linux",
        };
        var arch = architecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => "unsupported",
        };
        return os + "-" + arch;
    }
}
