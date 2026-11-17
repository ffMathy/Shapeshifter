using System.Text.Json.Nodes;
using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Tests.Settings;

public sealed class ShapeshifterSettingsTests
{
    [Fact]
    public void Defaults_AreDocumentedValues()
    {
        var settings = new ShapeshifterSettings();

        Assert.Equal(300, settings.PasteDelayMilliseconds);
        Assert.False(settings.QuietMode);
        Assert.Equal(50, settings.MaxHistoryItems);
        Assert.True(settings.AutomaticUpdates);
        Assert.False(settings.RememberHistory);
        Assert.Equal(3000, ShapeshifterSettings.MaxPasteDelayMilliseconds);
    }

    [Fact]
    public void Defaults_AreAlreadyNormalized()
    {
        var settings = new ShapeshifterSettings();

        Assert.Equal(settings, settings.Normalize());
    }

    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2999, 2999)]
    [InlineData(3000, 3000)]
    [InlineData(3001, 3000)]
    [InlineData(int.MaxValue, 3000)]
    public void Normalize_ClampsPasteDelay(int value, int expected) =>
        Assert.Equal(expected, new ShapeshifterSettings { PasteDelayMilliseconds = value }.Normalize().PasteDelayMilliseconds);

    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(int.MaxValue, 1000)]
    public void Normalize_ClampsHistorySize(int value, int expected) =>
        Assert.Equal(expected, new ShapeshifterSettings { MaxHistoryItems = value }.Normalize().MaxHistoryItems);

    [Fact]
    public void Normalize_KeepsQuietMode_AndDoesNotMutateTheOriginal()
    {
        var original = new ShapeshifterSettings { QuietMode = true, PasteDelayMilliseconds = -10, MaxHistoryItems = 5000 };

        var normalized = original.Normalize();

        Assert.True(normalized.QuietMode);
        Assert.Equal(-10, original.PasteDelayMilliseconds);
        Assert.Equal(5000, original.MaxHistoryItems);
        Assert.Equal(normalized, normalized.Normalize());
    }
}

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("shapeshifter-settings-").FullName;

    private string SettingsPath => Path.Combine(directory, "config", "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void MissingFile_UsesDefaults_AndDoesNotCreateTheFile()
    {
        var store = new SettingsStore(SettingsPath);

        Assert.Equal(new ShapeshifterSettings(), store.Current);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void Update_PersistsAndCreatesTheDirectory()
    {
        var store = new SettingsStore(SettingsPath);

        store.Update(settings => settings with { QuietMode = true, PasteDelayMilliseconds = 750, MaxHistoryItems = 123 });

        Assert.True(File.Exists(SettingsPath));
        Assert.False(File.Exists(SettingsPath + ".tmp"));
        var reloaded = new SettingsStore(SettingsPath).Current;
        Assert.Equal(new ShapeshifterSettings { QuietMode = true, PasteDelayMilliseconds = 750, MaxHistoryItems = 123 }, reloaded);
        Assert.Equal(reloaded, store.Current);
    }

    [Fact]
    public void Update_WritesReadableJson()
    {
        var store = new SettingsStore(SettingsPath);

        store.Update(settings => settings with { MaxHistoryItems = 77 });

        var json = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        Assert.Equal(77, json["MaxHistoryItems"]!.GetValue<int>());
        Assert.Equal(300, json["PasteDelayMilliseconds"]!.GetValue<int>());
        Assert.False(json["QuietMode"]!.GetValue<bool>());
    }

    [Fact]
    public void Update_NormalizesBeforeStoringAndPersisting()
    {
        var store = new SettingsStore(SettingsPath);

        store.Update(settings => settings with { PasteDelayMilliseconds = 99_999, MaxHistoryItems = 0 });

        Assert.Equal(3000, store.Current.PasteDelayMilliseconds);
        Assert.Equal(1, store.Current.MaxHistoryItems);
        Assert.Equal(store.Current, new SettingsStore(SettingsPath).Current);
    }

    [Fact]
    public void Update_PassesTheCurrentValueToTheChange()
    {
        var store = new SettingsStore(SettingsPath);
        store.Update(settings => settings with { MaxHistoryItems = 10 });
        ShapeshifterSettings? seen = null;

        store.Update(settings =>
        {
            seen = settings;
            return settings with { MaxHistoryItems = settings.MaxHistoryItems + 1 };
        });

        Assert.Equal(10, seen!.MaxHistoryItems);
        Assert.Equal(11, store.Current.MaxHistoryItems);
    }

    [Fact]
    public void Changed_IsRaisedOnceWithTheNewValue()
    {
        var store = new SettingsStore(SettingsPath);
        var raised = new List<(object? Sender, ShapeshifterSettings Settings)>();
        store.Changed += (sender, settings) => raised.Add((sender, settings));

        store.Update(settings => settings with { QuietMode = true });

        var (sender, value) = Assert.Single(raised);
        Assert.Same(store, sender);
        Assert.True(value.QuietMode);
        Assert.Same(store.Current, value);
    }

    [Fact]
    public void Changed_IsNotRaisedForAnUnchangedValue_AndNothingIsWritten()
    {
        var store = new SettingsStore(SettingsPath);
        var raised = 0;
        store.Changed += (_, _) => raised++;

        store.Update(settings => settings);
        store.Update(settings => settings with { });
        store.Update(_ => new ShapeshifterSettings());

        Assert.Equal(0, raised);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void Changed_IsNotRaisedWhenTheChangeNormalizesToTheCurrentValue()
    {
        var store = new SettingsStore(SettingsPath);
        store.Update(settings => settings with { PasteDelayMilliseconds = 3000 });
        var raised = 0;
        store.Changed += (_, _) => raised++;

        store.Update(settings => settings with { PasteDelayMilliseconds = 5000 });

        Assert.Equal(0, raised);
        Assert.Equal(3000, store.Current.PasteDelayMilliseconds);
    }

    [Fact]
    public void Changed_HandlerSeesTheUpdatedCurrentValue()
    {
        var store = new SettingsStore(SettingsPath);
        ShapeshifterSettings? currentInHandler = null;
        store.Changed += (_, _) => currentInHandler = store.Current;

        store.Update(settings => settings with { MaxHistoryItems = 9 });

        Assert.Equal(9, currentInHandler!.MaxHistoryItems);
    }

    [Fact]
    public void Update_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new SettingsStore(SettingsPath).Update(null!));

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"PasteDelayMilliseconds\": \"fast\"}")]
    [InlineData("{\"QuietMode\": 1}")]
    public void CorruptFile_FallsBackToDefaults(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, content);

        var store = new SettingsStore(SettingsPath);

        Assert.Equal(new ShapeshifterSettings(), store.Current);
    }

    [Fact]
    public void CorruptFile_IsReplacedOnTheNextUpdate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");
        var store = new SettingsStore(SettingsPath);

        store.Update(settings => settings with { QuietMode = true });

        Assert.True(new SettingsStore(SettingsPath).Current.QuietMode);
    }

    [Fact]
    public void PartialFile_KeepsDefaultsForMissingProperties()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{\"QuietMode\": true, \"Unknown\": 42}");

        var settings = new SettingsStore(SettingsPath).Current;

        Assert.True(settings.QuietMode);
        Assert.Equal(300, settings.PasteDelayMilliseconds);
        Assert.Equal(50, settings.MaxHistoryItems);
        Assert.True(settings.AutomaticUpdates);
    }

    [Fact]
    public void OutOfRangeFile_IsNormalizedOnLoad()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{\"PasteDelayMilliseconds\": -50, \"MaxHistoryItems\": 1000000}");

        var settings = new SettingsStore(SettingsPath).Current;

        Assert.Equal(0, settings.PasteDelayMilliseconds);
        Assert.Equal(1000, settings.MaxHistoryItems);
    }

    [Fact]
    public void UnwritableLocation_KeepsTheValueInMemory()
    {
        // A file where the settings directory should be makes saving fail.
        var blocker = Path.Combine(directory, "blocked");
        File.WriteAllText(blocker, "x");
        var store = new SettingsStore(Path.Combine(blocker, "settings.json"));
        var raised = 0;
        store.Changed += (_, _) => raised++;

        store.Update(settings => settings with { QuietMode = true });

        Assert.True(store.Current.QuietMode);
        Assert.Equal(1, raised);
    }
}
