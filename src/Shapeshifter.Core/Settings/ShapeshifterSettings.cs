namespace Shapeshifter.Core.Settings;

/// <summary>The keys held for the paste gesture (issue #28).</summary>
public enum PasteShortcut
{
    /// <summary>The platform's paste keys: Ctrl+V, or Cmd+V on macOS. A quick press still pastes as usual.</summary>
    Standard,

    /// <summary>
    /// Win+V on Windows (in place of the Windows clipboard history) or Ctrl+V on macOS, which leaves the paste keys
    /// alone. Linux has no alternative; there it acts as <see cref="Standard"/>.
    /// </summary>
    Alternative,
}

/// <summary>User preferences. Persisted as JSON by <see cref="SettingsStore"/>.</summary>
public sealed record ShapeshifterSettings
{
    public const int MaxPasteDelayMilliseconds = 3000;

    /// <summary>How long the modifier must be held after V before the picker shows up.</summary>
    public int PasteDelayMilliseconds { get; init; } = 300;

    /// <summary>Which keys open the history. Saved as a number, so an unknown value falls back to the standard keys.</summary>
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

    public ShapeshifterSettings Normalize() => this with
    {
        PasteDelayMilliseconds = Math.Clamp(PasteDelayMilliseconds, 0, MaxPasteDelayMilliseconds),
        PasteShortcut = Enum.IsDefined(PasteShortcut) ? PasteShortcut : PasteShortcut.Standard,
        MaxHistoryItems = Math.Clamp(MaxHistoryItems, 1, 1000),
    };
}
