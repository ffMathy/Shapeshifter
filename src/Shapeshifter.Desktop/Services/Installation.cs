using System.ComponentModel;
using System.Diagnostics;
using Serilog;
using Shapeshifter.Core.Settings;
using Shapeshifter.Core.Updates;

namespace Shapeshifter.Desktop.Services;

/// <summary>How Shapeshifter installs and updates itself (see <see cref="SelfInstaller"/> and <see cref="Updater"/>).</summary>
internal static class Installation
{
    public static readonly AppIdentity App = new()
    {
        Name = "Shapeshifter",
        LinuxExecutableName = "shapeshifter",
        Description = "Clipboard manager",
        GitHubOwner = "ffMathy",
        GitHubRepository = "Shapeshifter",
        // Release assets, see .github/workflows/ci.yml: Shapeshifter-win-x64.exe, Shapeshifter-linux-arm64, …
        ExecutableAssetName = rid => "Shapeshifter-" + rid + (rid.StartsWith("win-", StringComparison.Ordinal) ? ".exe" : string.Empty),
        DesktopEntryId = "shapeshifter",
        DesktopEntryLines = ["Categories=Utility;", "StartupWMClass=Shapeshifter", "X-GNOME-UsesNotifications=true"],
        BundleIdentifier = "app.shapeshifter.Shapeshifter",
        EnvironmentPrefix = "SHAPESHIFTER",
        // Embedded separately (see the project file): the installer runs before Avalonia can load its assets.
        OpenIconPng = () => typeof(Installation).Assembly.GetManifestResourceStream("Shapeshifter.Application.png"),
    };

    public static InstallLayout Layout(AppPaths paths) => InstallLayout.ForCurrentUser(App, paths.DataDirectory);

    /// <summary>Installs this copy when it was started from elsewhere. See <see cref="SelfInstaller.OnStartup"/>.</summary>
    public static StartupResult OnStartup(string[] args, AppPaths paths)
    {
        var layout = Layout(paths);
        var installer = new SelfInstaller(App, layout, RunningApp.Current(), new Host(layout), new UpdateLog(Log.ForContext(typeof(Installation))));
        return installer.OnStartup(args);
    }

    /// <summary>Writes the installer's and the updater's messages to the Serilog log.</summary>
    internal sealed class UpdateLog(ILogger logger) : IUpdateLog
    {
        public void Information(string message) => logger.Information("{Message}", message);

        public void Warning(string message, Exception? exception = null) => logger.Warning(exception, "{Message}", message);
    }

    private sealed class Host(InstallLayout layout) : IInstallHost
    {
        /// <summary>
        /// Shapeshifter has no channel to ask a running copy to quit, so the installed copy is ended (its history of
        /// unpinned items lives in memory only; pinned items are on disk).
        /// </summary>
        public void StopRunningInstance()
        {
            var self = Environment.ProcessId;
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        if (process.Id == self || process.MainModule?.FileName is not { } file || !IsInstalledExecutable(file))
                        {
                            continue;
                        }

                        Log.Information("Ending the running copy of Shapeshifter (process {Pid})", process.Id);
                        process.Kill();
                        process.WaitForExit(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
                    {
                        // Another user's process, or one that already exited.
                    }
                }
            }
        }

        public void RemoveAutostart()
        {
            var autostart = Platform.PlatformServices.CreateAutostart();
            if (autostart.IsSupported && autostart.IsEnabled)
            {
                autostart.SetEnabled(false);
            }
        }

        /// <summary>The installed executable, also when Windows still runs it under its renamed <c>.old-…</c> name.</summary>
        private bool IsInstalledExecutable(string file) =>
            layout.SamePath(file, layout.Executable)
            || (Path.GetFileName(file).StartsWith(Path.GetFileName(layout.Executable) + ".old-", StringComparison.OrdinalIgnoreCase)
                && layout.SamePath(Path.GetDirectoryName(file)!, Path.GetDirectoryName(layout.Executable)!));
    }
}
