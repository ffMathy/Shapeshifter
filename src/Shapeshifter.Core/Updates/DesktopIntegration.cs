using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace Shapeshifter.Core.Updates;

/// <summary>
/// Makes an installed application findable like any other: a Start menu shortcut and an entry in Apps &amp; features
/// and Control Panel's Programs and Features (per user, under <c>HKCU\...\Uninstall</c>, which runs
/// <c>--uninstall</c>) on Windows, a desktop entry and icon on Linux, and the app bundle's <c>Info.plist</c> on macOS.
/// </summary>
public sealed class DesktopIntegration
{
    /// <summary>Marks an Info.plist the installer wrote, so it is only ever rewritten when it is one of ours.</summary>
    internal const string GeneratedPlistMarker = "<!-- Written by the installer. -->";

    private readonly AppIdentity app;
    private readonly InstallLayout layout;
    private readonly IUpdateLog log;

    public DesktopIntegration(AppIdentity app, InstallLayout layout, IUpdateLog log)
    {
        this.app = app;
        this.layout = layout;
        this.log = log;
    }

    /// <summary>The desktop entry on Linux.</summary>
    public string DesktopEntryPath => Path.Combine(layout.DataHome, "applications", app.DesktopEntryId + ".desktop");

    /// <summary>Creates or repairs the shortcuts of the installed copy. Never throws.</summary>
    public void Register(ReleaseVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (!layout.IsForCurrentUser && layout.Platform != InstallPlatform.MacOS)
        {
            return;
        }

        Try("register with the desktop", () =>
        {
            switch (layout.Platform)
            {
                case InstallPlatform.Windows when OperatingSystem.IsWindows():
                    RegisterWindows(version);
                    break;
                case InstallPlatform.MacOS:
                    RegisterMac(version);
                    break;
                case InstallPlatform.Linux:
                    RegisterLinux();
                    break;
            }
        });
    }

    /// <summary>Removes the shortcuts. Never throws.</summary>
    public void Unregister()
    {
        if (!layout.IsForCurrentUser)
        {
            return;
        }

        Try("remove the shortcuts", () =>
        {
            if (layout.Platform == InstallPlatform.Windows && OperatingSystem.IsWindows())
            {
                File.Delete(StartMenuShortcut);
                Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false);
            }
            else if (layout.Platform == InstallPlatform.Linux)
            {
                File.Delete(DesktopEntryPath);
                foreach (var icon in Directory.Exists(IconsRoot) ? Directory.EnumerateFiles(IconsRoot, app.DesktopEntryId + ".png", SearchOption.AllDirectories) : [])
                {
                    File.Delete(icon);
                }
            }
        });
    }

    // ------------------------------------------------------------------ Linux

    private string IconsRoot => Path.Combine(layout.DataHome, "icons", "hicolor");

    private void RegisterLinux()
    {
        var iconName = WriteLinuxIcon() ? app.DesktopEntryId : null;
        WriteIfChanged(DesktopEntryPath, BuildDesktopEntry(app, layout.Executable, iconName));
    }

    internal static string BuildDesktopEntry(AppIdentity app, string executable, string? iconName)
    {
        var exec = string.Join(' ', new[] { executable }.Concat(app.LaunchArguments).Select(QuoteExecArgument));
        var text = new StringBuilder()
            .Append("[Desktop Entry]\n")
            .Append("Type=Application\n")
            .Append("Name=").Append(app.Name).Append('\n')
            .Append("Comment=").Append(app.Description).Append('\n')
            .Append("Exec=").Append(exec).Append('\n')
            .Append("TryExec=").Append(executable).Append('\n');
        if (iconName is not null)
        {
            text.Append("Icon=").Append(iconName).Append('\n');
        }

        text.Append("Terminal=false\n");
        foreach (var line in app.DesktopEntryLines)
        {
            text.Append(line).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>
    /// Quotes one argument for a desktop entry's Exec key: in double quotes with <c>" ` $ \</c> escaped, then
    /// backslashes escaped again (string values) and <c>%</c> doubled (field codes).
    /// </summary>
    internal static string QuoteExecArgument(string argument)
    {
        var quoted = new StringBuilder(argument.Length + 2).Append('"');
        foreach (var c in argument)
        {
            if (c is '"' or '`' or '$' or '\\')
            {
                quoted.Append('\\');
            }

            quoted.Append(c);
        }

        quoted.Append('"');
        return quoted.ToString().Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "%%", StringComparison.Ordinal);
    }

    private bool WriteLinuxIcon()
    {
        using var source = app.OpenIconPng?.Invoke();
        if (source is null)
        {
            return false;
        }

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var png = buffer.ToArray();
        var size = PngWidth(png);
        if (size is null)
        {
            return false;
        }

        var path = Path.Combine(IconsRoot, $"{size}x{size}", "apps", app.DesktopEntryId + ".png");
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(png))
        {
            return true;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, png);
        return true;
    }

    /// <summary>The width of a PNG image, from its header; null when the bytes are not a PNG.</summary>
    internal static int? PngWidth(ReadOnlySpan<byte> png)
    {
        ReadOnlySpan<byte> signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (png.Length < 24 || !png[..8].SequenceEqual(signature) || !png[12..16].SequenceEqual("IHDR"u8))
        {
            return null;
        }

        return (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
    }

    // ------------------------------------------------------------------ macOS

    private void RegisterMac(ReleaseVersion version)
    {
        var plist = Path.Combine(layout.Target, "Contents", "Info.plist");
        if (File.Exists(plist) && !File.ReadAllText(plist).Contains(GeneratedPlistMarker, StringComparison.Ordinal))
        {
            // The release's own bundle: it already describes itself.
            return;
        }

        WriteIfChanged(plist, BuildInfoPlist(app, version));
        WriteIfChanged(Path.Combine(layout.Target, "Contents", "PkgInfo"), "APPL????");
    }

    internal static string BuildInfoPlist(AppIdentity app, ReleaseVersion version)
    {
        var name = SecurityElement.Escape(app.Name);
        var number = SecurityElement.Escape(version.ToString());
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            {GeneratedPlistMarker}
            <plist version="1.0">
            <dict>
              <key>CFBundleName</key>
              <string>{name}</string>
              <key>CFBundleDisplayName</key>
              <string>{name}</string>
              <key>CFBundleIdentifier</key>
              <string>{SecurityElement.Escape(app.BundleIdentifier)}</string>
              <key>CFBundleExecutable</key>
              <string>{name}</string>
              <key>CFBundlePackageType</key>
              <string>APPL</string>
              <key>CFBundleShortVersionString</key>
              <string>{number}</string>
              <key>CFBundleVersion</key>
              <string>{number}</string>
              <key>LSMinimumSystemVersion</key>
              <string>12.0</string>
              <key>LSUIElement</key>
              <true/>
              <key>NSHighResolutionCapable</key>
              <true/>
            </dict>
            </plist>

            """;
    }

    // ------------------------------------------------------------------ Windows

    private string UninstallKey => @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + app.Name;

    private string StartMenuShortcut => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", app.Name + ".lnk");

    [SupportedOSPlatform("windows")]
    private void RegisterWindows(ReleaseVersion version)
    {
        var executable = layout.Executable;
        using (var key = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            key.SetValue("DisplayName", app.Name);
            key.SetValue("DisplayVersion", version.ToString());
            key.SetValue("DisplayIcon", executable);
            key.SetValue("Publisher", app.GitHubOwner);
            key.SetValue("Comments", app.Description);
            key.SetValue("InstallLocation", Path.GetDirectoryName(executable)!);
            key.SetValue("UninstallString", $"\"{executable}\" {SelfInstaller.UninstallFlag}");
            key.SetValue("QuietUninstallString", $"\"{executable}\" {SelfInstaller.UninstallFlag}");
            key.SetValue("URLInfoAbout", $"https://github.com/{app.GitHubOwner}/{app.GitHubRepository}");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            var kilobytes = File.Exists(executable) ? (int)Math.Min(int.MaxValue, new FileInfo(executable).Length / 1024) : 0;
            key.SetValue("EstimatedSize", kilobytes, RegistryValueKind.DWord);
        }

        if (!File.Exists(StartMenuShortcut))
        {
            CreateShortcut(StartMenuShortcut, executable, string.Join(' ', app.LaunchArguments), app.Description);
        }
    }

    /// <summary>
    /// Creates a .lnk through Windows PowerShell's WScript.Shell. The paths travel in environment variables, so no
    /// quoting is involved.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void CreateShortcut(string shortcut, string target, string arguments, string description)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcut)!);
        const string Script =
            "$s = (New-Object -ComObject WScript.Shell).CreateShortcut($env:SC_LINK); " +
            "$s.TargetPath = $env:SC_TARGET; $s.Arguments = $env:SC_ARGS; " +
            "$s.WorkingDirectory = (Split-Path -Parent $env:SC_TARGET); $s.IconLocation = $env:SC_TARGET; " +
            "$s.Description = $env:SC_DESCRIPTION; $s.Save()";
        var powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var start = new ProcessStartInfo(powershell)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", Script })
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["SC_LINK"] = shortcut;
        start.Environment["SC_TARGET"] = target;
        start.Environment["SC_ARGS"] = arguments;
        start.Environment["SC_DESCRIPTION"] = description;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start PowerShell.");
        var error = process.StandardError.ReadToEndAsync();
        _ = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill();
            throw new InvalidOperationException("PowerShell did not create the Start menu shortcut in time.");
        }

        if (process.ExitCode != 0 || !File.Exists(shortcut))
        {
            throw new InvalidOperationException("PowerShell could not create the Start menu shortcut: " + error.Result.Trim());
        }
    }

    // ------------------------------------------------------------------ helpers

    private static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path) == content)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporary, path, overwrite: true);
    }

    private void Try(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            log.Warning($"Could not {what}", exception);
        }
    }
}
