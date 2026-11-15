using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Platform.Common;

/// <summary>The command line that starts this application again.</summary>
internal static class StartCommand
{
    public static IReadOnlyList<string> Get()
    {
        var process = Environment.ProcessPath ?? throw new InvalidOperationException("The executable path is unknown.");
        var name = Path.GetFileNameWithoutExtension(process);
        if (string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            // Started as "dotnet Shapeshifter.dll" (framework-dependent build).
            var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entry))
            {
                return [process, entry];
            }
        }

        return [process];
    }
}

[SupportedOSPlatform("windows")]
internal sealed class WindowsAutostartService : IAutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Shapeshifter";

    public bool IsSupported => true;

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue(ValueName, string.Join(' ', StartCommand.Get().Select(part => $"\"{part}\"")));
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

/// <summary>freedesktop.org autostart entry in <c>~/.config/autostart</c>.</summary>
internal sealed class XdgAutostartService : IAutostartService
{
    private readonly string path;

    public XdgAutostartService()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var root = string.IsNullOrWhiteSpace(config)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : config;
        path = Path.Combine(root, "autostart", "shapeshifter.desktop");
    }

    public bool IsSupported => true;

    public bool IsEnabled => File.Exists(path);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(path);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var exec = string.Join(' ', StartCommand.Get().Select(Quote));
        File.WriteAllText(path, $"""
            [Desktop Entry]
            Type=Application
            Name=Shapeshifter
            Comment=Clipboard manager
            Exec={exec}
            Terminal=false
            X-GNOME-Autostart-enabled=true

            """);
    }

    private static string Quote(string argument) =>
        "\"" + argument.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}

/// <summary>A per-user launchd agent in <c>~/Library/LaunchAgents</c>.</summary>
internal sealed class LaunchAgentAutostartService : IAutostartService
{
    private const string Label = "app.shapeshifter.desktop";
    private readonly string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", Label + ".plist");

    public bool IsSupported => true;

    public bool IsEnabled => File.Exists(path);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(path);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var arguments = string.Concat(StartCommand.Get().Select(part => $"\n        <string>{SecurityElement.Escape(part)}</string>"));
        File.WriteAllText(path, $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{Label}</string>
                <key>ProgramArguments</key>
                <array>{arguments}
                </array>
                <key>RunAtLoad</key>
                <true/>
                <key>ProcessType</key>
                <string>Interactive</string>
            </dict>
            </plist>

            """);
    }
}

internal sealed class UnsupportedAutostartService : IAutostartService
{
    public bool IsSupported => false;

    public bool IsEnabled => false;

    public void SetEnabled(bool enabled) => throw new PlatformNotSupportedException();
}
