namespace Shapeshifter.Core.Settings;

/// <summary>The keys held for the paste gesture (issue #28).</summary>
public enum PasteShortcut
{
    /// <summary>Ctrl+V, or Cmd+V on macOS: a quick press still pastes as usual.</summary>
    ControlV,

    /// <summary>
    /// Win+V, in place of the Windows clipboard history, so Ctrl+V is left alone. Only available on Windows; elsewhere
    /// it acts as <see cref="ControlV"/>.
    /// </summary>
    WindowsV,
}

/// <summary>User preferences. Persisted as JSON by <see cref="SettingsStore"/>.</summary>
public sealed record ShapeshifterSettings
{
    public const int MaxPasteDelayMilliseconds = 3000;

    /// <summary>How long the modifier must be held after V before the picker shows up.</summary>
    public int PasteDelayMilliseconds { get; init; } = 300;

    /// <summary>Which keys open the history. Saved as a number, so an unknown value falls back to Ctrl+V.</summary>
    public PasteShortcut PasteShortcut { get; init; }

    /// <summary>When set, no "copied" notification is shown after each copy.</summary>
    public bool QuietMode { get; init; }

    /// <summary>
    /// Whether to show a tip, in place of the "copied" notification, when Shapeshifter could have saved the user a
    /// copy: something copied again that is already in the history, or copying back and forth between two applications.
    /// </summary>
    public bool CoachingTips { get; init; } = true;

    /// <summary>How many unpinned items are kept.</summary>
    public int MaxHistoryItems { get; init; } = 50;

    /// <summary>
    /// Whether the unpinned history is also kept on disk, so it survives restarts. Off by default: then only pinned
    /// items are ever written to disk.
    /// </summary>
    public bool RememberHistory { get; init; }

    /// <summary>Whether new releases are downloaded from GitHub and installed automatically.</summary>
    public bool AutomaticUpdates { get; init; } = true;

    /// <summary>The online services (such as Catbox) the user has agreed to upload images to.</summary>
    public IReadOnlyList<string> UploadConsents { get; init; } = [];

    /// <summary>
    /// Compares the lists by their contents, so settings read back from disk equal the ones that were saved. Every
    /// property must be compared here: <see cref="SettingsStore"/> skips saving a change that compares equal.
    /// </summary>
    public bool Equals(ShapeshifterSettings? other) =>
        other is not null
        && PasteDelayMilliseconds == other.PasteDelayMilliseconds
        && PasteShortcut == other.PasteShortcut
        && QuietMode == other.QuietMode
        && MaxHistoryItems == other.MaxHistoryItems
        && RememberHistory == other.RememberHistory
        && AutomaticUpdates == other.AutomaticUpdates
        && CoachingTips == other.CoachingTips
        && (UploadConsents ?? []).SequenceEqual(other.UploadConsents ?? [], StringComparer.Ordinal);

    public override int GetHashCode() =>
        HashCode.Combine(PasteDelayMilliseconds, PasteShortcut, QuietMode, MaxHistoryItems, RememberHistory, AutomaticUpdates, CoachingTips, UploadConsents?.Count ?? 0);

    public ShapeshifterSettings Normalize() => this with
    {
        PasteDelayMilliseconds = Math.Clamp(PasteDelayMilliseconds, 0, MaxPasteDelayMilliseconds),
        PasteShortcut = Enum.IsDefined(PasteShortcut) ? PasteShortcut : PasteShortcut.ControlV,
        MaxHistoryItems = Math.Clamp(MaxHistoryItems, 1, 1000),
        UploadConsents = UploadConsents ?? [],
    };
}
