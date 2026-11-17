namespace Shapeshifter.Core.Settings;

/// <summary>User preferences. Persisted as JSON by <see cref="SettingsStore"/>.</summary>
public sealed record ShapeshifterSettings
{
    public const int MaxPasteDelayMilliseconds = 3000;

    /// <summary>How long the modifier must be held after V before the picker shows up.</summary>
    public int PasteDelayMilliseconds { get; init; } = 300;

    /// <summary>When set, no "copied" notification is shown after each copy.</summary>
    public bool QuietMode { get; init; }

    /// <summary>How many unpinned items are kept.</summary>
    public int MaxHistoryItems { get; init; } = 50;

    /// <summary>Whether new releases are downloaded from GitHub and installed automatically.</summary>
    public bool AutomaticUpdates { get; init; } = true;

    /// <summary>The online services (such as Catbox) the user has agreed to upload images to.</summary>
    public IReadOnlyList<string> UploadConsents { get; init; } = [];

    /// <summary>Compares the lists by their contents, so settings read back from disk equal the ones that were saved.</summary>
    public bool Equals(ShapeshifterSettings? other) =>
        other is not null
        && PasteDelayMilliseconds == other.PasteDelayMilliseconds
        && QuietMode == other.QuietMode
        && MaxHistoryItems == other.MaxHistoryItems
        && AutomaticUpdates == other.AutomaticUpdates
        && (UploadConsents ?? []).SequenceEqual(other.UploadConsents ?? [], StringComparer.Ordinal);

    public override int GetHashCode() =>
        HashCode.Combine(PasteDelayMilliseconds, QuietMode, MaxHistoryItems, AutomaticUpdates, UploadConsents?.Count ?? 0);

    public ShapeshifterSettings Normalize() => this with
    {
        PasteDelayMilliseconds = Math.Clamp(PasteDelayMilliseconds, 0, MaxPasteDelayMilliseconds),
        MaxHistoryItems = Math.Clamp(MaxHistoryItems, 1, 1000),
        UploadConsents = UploadConsents ?? [],
    };
}
