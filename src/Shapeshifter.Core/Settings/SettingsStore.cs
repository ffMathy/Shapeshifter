using System.Text.Json;
using Serilog;

namespace Shapeshifter.Core.Settings;

/// <summary>Loads and saves <see cref="ShapeshifterSettings"/> as JSON.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string path;
    private readonly ILogger logger;
    private readonly Lock gate = new();
    private ShapeshifterSettings current;

    public SettingsStore(string path, ILogger? logger = null)
    {
        this.path = path;
        this.logger = logger ?? Log.Logger;
        current = Load();
    }

    public event EventHandler<ShapeshifterSettings>? Changed;

    public ShapeshifterSettings Current
    {
        get
        {
            lock (gate)
            {
                return current;
            }
        }
    }

    public void Update(Func<ShapeshifterSettings, ShapeshifterSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        ShapeshifterSettings updated;
        lock (gate)
        {
            updated = change(current).Normalize();
            if (updated == current)
            {
                return;
            }

            current = updated;
            Save(updated);
        }

        Changed?.Invoke(this, updated);
    }

    private ShapeshifterSettings Load()
    {
        try
        {
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<ShapeshifterSettings>(File.ReadAllText(path), JsonOptions);
                if (loaded is not null)
                {
                    return loaded.Normalize();
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.Warning(exception, "Could not read settings from {Path}; using defaults", path);
        }

        return new ShapeshifterSettings();
    }

    private void Save(ShapeshifterSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.Error(exception, "Could not save settings to {Path}", path);
        }
    }
}
