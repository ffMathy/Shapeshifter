using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shapeshifter.Core.Updates;

/// <summary>What the application does when it can stop another running copy of itself.</summary>
public interface IInstallHost
{
    /// <summary>
    /// Asks a running copy of the application to exit and waits for it, before its executable is replaced or
    /// removed. Returns without doing anything when no copy runs.
    /// </summary>
    void StopRunningInstance();

    /// <summary>Removes the start-at-login entry (when uninstalling).</summary>
    void RemoveAutostart();
}

/// <summary>What <see cref="SelfInstaller.OnStartup"/> decided.</summary>
/// <param name="Exit">True when this process is done (the installed copy was started, or it uninstalled).</param>
/// <param name="Arguments">The command line without the installer's own options.</param>
/// <param name="IsInstalledCopy">True when this process runs from the install location.</param>
/// <param name="FirstInstall">True on the first start after the application was installed on this computer.</param>
public sealed record StartupResult(bool Exit, string[] Arguments, bool IsInstalledCopy, bool FirstInstall);

/// <summary>
/// Installs the application for the current user the first time it is started from anywhere else (a download folder,
/// a USB stick), then starts the installed copy. A newer download started later replaces the installation; an older
/// one starts the installed, newer copy instead.
/// </summary>
/// <remarks>
/// Only single-file release builds install themselves (see <see cref="RunningApp.IsSingleFile"/>); pull request builds
/// (<see cref="ReleaseVersion.IsPullRequestBuild"/>) run where they are. Start with
/// <c>--no-install</c>, or set <c>NAME_NO_INSTALL=1</c>, to run a copy where it is. <c>--uninstall</c> removes the
/// installation (the user's settings and data stay).
/// </remarks>
public sealed class SelfInstaller
{
    public const string NoInstallFlag = "--no-install";
    public const string UninstallFlag = "--uninstall";
    public const string ReplacesOption = "--replaces";
    public const string FirstInstallFlag = "--first-install";

    private static readonly TimeSpan WaitForReplacedProcess = TimeSpan.FromSeconds(30);

    private readonly AppIdentity app;
    private readonly InstallLayout layout;
    private readonly RunningApp running;
    private readonly IInstallHost host;
    private readonly IUpdateLog log;
    private readonly ReleaseVersion version;

    public SelfInstaller(AppIdentity app, InstallLayout layout, RunningApp running, IInstallHost host, IUpdateLog log, ReleaseVersion? version = null)
    {
        this.app = app;
        this.layout = layout;
        this.running = running;
        this.host = host;
        this.log = log;
        this.version = version ?? ReleaseVersion.Current();
        Desktop = new DesktopIntegration(app, layout, log);
    }

    public DesktopIntegration Desktop { get; }

    /// <summary>Starts a process without a shell (paths with spaces and quotes pass through as they are).</summary>
    public static Process? Launch(string executable, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return Process.Start(start);
    }

    /// <summary>Removes the installer's options from <paramref name="args"/> and reads them.</summary>
    internal static (string[] Remaining, int? Replaces, bool NoInstall, bool Uninstall, bool FirstInstall) ParseArguments(IEnumerable<string> args)
    {
        var remaining = new List<string>();
        int? replaces = null;
        bool noInstall = false, uninstall = false, firstInstall = false;
        using var enumerator = args.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var argument = enumerator.Current;
            switch (argument)
            {
                case NoInstallFlag:
                    noInstall = true;
                    break;
                case UninstallFlag:
                    uninstall = true;
                    break;
                case FirstInstallFlag:
                    firstInstall = true;
                    break;
                case ReplacesOption:
                    if (enumerator.MoveNext() && int.TryParse(enumerator.Current, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
                    {
                        replaces = pid;
                    }

                    break;
                default:
                    remaining.Add(argument);
                    break;
            }
        }

        return (remaining.ToArray(), replaces, noInstall, uninstall, firstInstall);
    }

    /// <summary>Runs first thing in <c>Main</c>, before the single-instance check. Never throws.</summary>
    public StartupResult OnStartup(string[] args)
    {
        var (arguments, replaces, noInstall, uninstall, firstInstall) = ParseArguments(args);
        if (replaces is { } pid)
        {
            // Started by an update: let the old copy finish exiting and release the single-instance lock.
            WaitForExit(pid);
        }

        if (!running.IsSingleFile || running.IsStoreInstall)
        {
            return new StartupResult(false, arguments, false, false);
        }

        var isInstalled = layout.IsInstalled(running);
        FileSwap.CleanLeftovers(running.ReplaceablePath);
        if (uninstall)
        {
            Uninstall();
            return new StartupResult(true, arguments, isInstalled, false);
        }

        if (version.IsPullRequestBuild && !isInstalled)
        {
            log.Information($"{app.Name} {version} is a pull request build: running it from {running.ExecutablePath} without installing it");
            return new StartupResult(false, arguments, false, false);
        }

        if (isInstalled)
        {
            Desktop.Register(version);
            return new StartupResult(false, arguments, true, firstInstall);
        }

        if (noInstall || Environment.GetEnvironmentVariable(app.NoInstallVariable) == "1")
        {
            log.Information($"Running from {running.ExecutablePath} without installing it");
            return new StartupResult(false, arguments, false, false);
        }

        try
        {
            var installed = ReadInstalledVersion();
            var exists = File.Exists(layout.Executable);
            if (exists && installed is not null && installed > version)
            {
                log.Information($"Version {installed} is already installed; starting it instead of {version}");
                using var _ = Launch(layout.Executable, arguments);
                return new StartupResult(true, arguments, false, false);
            }

            log.Information($"Installing {app.Name} {version} to {layout.Target}");
            host.StopRunningInstance();
            Install();
            using var started = Launch(layout.Executable, exists ? arguments : [.. arguments, FirstInstallFlag]);
            return new StartupResult(true, arguments, false, false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or NotSupportedException)
        {
            log.Warning($"Could not install {app.Name} to {layout.Target}; running from {running.ExecutablePath} instead", exception);
            return new StartupResult(false, arguments, false, false);
        }
    }

    /// <summary>Copies the running application to the install location and registers it.</summary>
    internal void Install()
    {
        var parent = Path.GetDirectoryName(layout.Target)!;
        Directory.CreateDirectory(parent);
        FileSwap.CleanLeftovers(layout.Target);
        var staged = FileSwap.StagingPath(layout.Target);
        try
        {
            if (layout.TargetIsBundle)
            {
                StageBundle(staged);
            }
            else
            {
                File.Copy(running.ExecutablePath, staged);
                FileSwap.MakeExecutable(staged);
            }

            FileSwap.RemoveDownloadMark(staged);
            FileSwap.Replace(staged, layout.Target);
        }
        finally
        {
            FileSwap.TryDelete(staged);
        }

        WriteInstalledVersion(layout, version);
        Desktop.Register(version);
    }

    /// <summary>The app bundle to install on macOS: a copy of the one running, or a new one around the executable.</summary>
    private void StageBundle(string staged)
    {
        if (running.BundlePath is { } bundle)
        {
            // ditto keeps what a bundle needs (symbolic links, permissions, the code signature).
            FileSwap.RunTool("/usr/bin/ditto", bundle, staged);
            return;
        }

        var executable = Path.Combine(staged, "Contents", "MacOS", app.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.Copy(running.ExecutablePath, executable);
        FileSwap.MakeExecutable(executable);
        File.WriteAllText(Path.Combine(staged, "Contents", "Info.plist"), DesktopIntegration.BuildInfoPlist(app, version));
        File.WriteAllText(Path.Combine(staged, "Contents", "PkgInfo"), "APPL????");
    }

    /// <summary>Removes the installation and its shortcuts; settings and data stay.</summary>
    internal void Uninstall()
    {
        log.Information($"Uninstalling {app.Name} from {layout.Target}");
        try
        {
            host.StopRunningInstance();
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or IOException)
        {
            log.Warning("Could not stop the running copy", exception);
        }

        try
        {
            host.RemoveAutostart();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or PlatformNotSupportedException or System.Security.SecurityException)
        {
            log.Warning("Could not remove the start-at-login entry", exception);
        }

        Desktop.Unregister();
        FileSwap.TryDelete(layout.StateFile);
        FileSwap.CleanLeftovers(layout.Target);
        if (FileSwap.TryDelete(layout.Target))
        {
            if (layout.Platform == InstallPlatform.Windows)
            {
                TryDeleteEmptyDirectory(Path.GetDirectoryName(layout.Target)!);
            }

            return;
        }

        if (layout.Platform == InstallPlatform.Windows)
        {
            // This process runs from the folder it removes: let cmd delete it once this process has exited.
            var folder = Path.GetDirectoryName(layout.Target)!;
            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"/d /c ping -n 4 127.0.0.1 >nul & rmdir /s /q \"{folder}\"",
            };
            try
            {
                using var _ = Process.Start(start);
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                log.Warning($"Could not remove {folder}", exception);
            }
        }
    }

    private static void TryDeleteEmptyDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private ReleaseVersion? ReadInstalledVersion() => ReadInstalledVersion(layout);

    /// <summary>The version the state file says is installed, or null when unknown.</summary>
    public static ReleaseVersion? ReadInstalledVersion(InstallLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        try
        {
            if (!File.Exists(layout.StateFile))
            {
                return null;
            }

            var state = JsonNode.Parse(File.ReadAllText(layout.StateFile));
            var path = state?["path"]?.GetValue<string>();
            if (path is null || !layout.SamePath(path, layout.Target))
            {
                return null;
            }

            return ReleaseVersion.TryParse(state?["version"]?.GetValue<string>());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Records which version is installed (after installing and after each update).</summary>
    public static void WriteInstalledVersion(InstallLayout layout, ReleaseVersion installed)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(installed);
        Directory.CreateDirectory(Path.GetDirectoryName(layout.StateFile)!);
        var state = new JsonObject
        {
            ["version"] = installed.ToString(),
            ["path"] = layout.Target,
        };
        var temporary = layout.StateFile + ".tmp";
        File.WriteAllText(temporary, state.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, layout.StateFile, overwrite: true);
    }

    private void WaitForExit(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!process.WaitForExit(WaitForReplacedProcess))
            {
                log.Warning($"The previous copy (process {pid}) is still running");
            }
        }
        catch (ArgumentException)
        {
            // Already gone.
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            log.Warning($"Could not wait for process {pid}", exception);
        }
    }
}
