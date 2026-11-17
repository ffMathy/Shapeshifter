using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shapeshifter.Core.Updates;

/// <summary>
/// The copy of the application that is running, as it describes itself in a small file next to its single-instance
/// lock: its process, version and executable. A newer copy that is started reads it to replace the running one
/// (<see cref="ShouldReplace"/>) rather than exit because one is already running.
/// </summary>
public sealed record RunningInstance(int ProcessId, ReleaseVersion Version, string ExecutablePath)
{
    /// <summary>Whether a copy of version <paramref name="launching"/> should end <paramref name="running"/> and run instead.</summary>
    public static bool ShouldReplace(ReleaseVersion launching, RunningInstance? running)
    {
        ArgumentNullException.ThrowIfNull(launching);
        return running is not null && launching > running.Version;
    }

    /// <summary>Records this instance; failing to is not fatal (a newer copy then cannot tell which version runs).</summary>
    public void Write(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var state = new JsonObject
        {
            ["pid"] = ProcessId,
            ["version"] = Version.ToString(),
            ["path"] = ExecutablePath,
        };
        var temporary = path + "." + ProcessId.ToString(CultureInfo.InvariantCulture) + ".tmp";
        File.WriteAllText(temporary, state.ToJsonString());
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>The instance recorded at <paramref name="path"/>, or null when there is none or it cannot be read.</summary>
    public static RunningInstance? Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var state = JsonNode.Parse(File.ReadAllText(path));
            var pid = state?["pid"]?.GetValue<int>();
            var version = ReleaseVersion.TryParse(state?["version"]?.GetValue<string>());
            var executable = state?["path"]?.GetValue<string>();
            return pid is > 0 && version is not null && !string.IsNullOrEmpty(executable)
                ? new RunningInstance(pid.Value, version, executable)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Removes the record when this instance exits, unless another instance has already replaced it.</summary>
    public static void Remove(string path, int processId)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (Read(path)?.ProcessId == processId)
        {
            FileSwap.TryDelete(path);
        }
    }
}
