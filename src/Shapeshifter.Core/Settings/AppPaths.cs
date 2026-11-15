namespace Shapeshifter.Core.Settings;

/// <summary>Per-user locations, following each platform's conventions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>Windows: <c>%APPDATA%\Shapeshifter</c> (settings) and <c>%LOCALAPPDATA%\Shapeshifter</c> (data, logs).</item>
/// <item>macOS: <c>~/Library/Application Support/Shapeshifter</c> and <c>~/Library/Logs/Shapeshifter</c>.</item>
/// <item>Linux: <c>$XDG_CONFIG_HOME/shapeshifter</c> and <c>$XDG_DATA_HOME/shapeshifter</c>.</item>
/// </list>
/// Setting the <c>SHAPESHIFTER_HOME</c> environment variable puts everything below that folder (used by tests).
/// </remarks>
public sealed class AppPaths
{
    public const string HomeOverrideVariable = "SHAPESHIFTER_HOME";

    public AppPaths(string configDirectory, string dataDirectory, string logDirectory, string tempDirectory)
    {
        ConfigDirectory = configDirectory;
        DataDirectory = dataDirectory;
        LogDirectory = logDirectory;
        TempDirectory = tempDirectory;
    }

    public string ConfigDirectory { get; }

    public string DataDirectory { get; }

    public string LogDirectory { get; }

    /// <summary>Scratch space for files produced by actions (text files, zip archives). Cleaned on startup.</summary>
    public string TempDirectory { get; }

    public string SettingsFile => Path.Combine(ConfigDirectory, "settings.json");

    public string PinnedDirectory => Path.Combine(DataDirectory, "Pinned");

    public string InstanceLockFile => Path.Combine(DataDirectory, "instance.lock");

    public static AppPaths ForRoot(string root) => new(
        Path.Combine(root, "config"),
        Path.Combine(root, "data"),
        Path.Combine(root, "logs"),
        Path.Combine(root, "temp"));

    public static AppPaths CreateDefault()
    {
        var overrideRoot = Environment.GetEnvironmentVariable(HomeOverrideVariable);
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            return ForRoot(Path.GetFullPath(overrideRoot));
        }

        var temp = Path.Combine(Path.GetTempPath(), "Shapeshifter-" + SanitizedUserName());
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new AppPaths(
                Path.Combine(roaming, "Shapeshifter"),
                Path.Combine(local, "Shapeshifter"),
                Path.Combine(local, "Shapeshifter", "Logs"),
                temp);
        }

        if (OperatingSystem.IsMacOS())
        {
            var support = Path.Combine(home, "Library", "Application Support", "Shapeshifter");
            return new AppPaths(support, support, Path.Combine(home, "Library", "Logs", "Shapeshifter"), temp);
        }

        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var configRoot = string.IsNullOrWhiteSpace(config) ? Path.Combine(home, ".config") : config;
        var dataRoot = string.IsNullOrWhiteSpace(data) ? Path.Combine(home, ".local", "share") : data;
        return new AppPaths(
            Path.Combine(configRoot, "shapeshifter"),
            Path.Combine(dataRoot, "shapeshifter"),
            Path.Combine(dataRoot, "shapeshifter", "logs"),
            temp);
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(TempDirectory);
    }

    private static string SanitizedUserName()
    {
        var name = Environment.UserName;
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
