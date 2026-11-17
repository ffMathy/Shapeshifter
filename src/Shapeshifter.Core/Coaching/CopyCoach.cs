using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Coaching;

/// <summary>Which habit a <see cref="CoachingTip"/> is about.</summary>
public enum CoachingTipKind
{
    /// <summary>Something already in the history was copied again (issue #284).</summary>
    CopiedAgain,

    /// <summary>
    /// The user copied from one application, pasted into another and went straight back to copy the next thing
    /// (issue #310), where they could have copied everything first and picked each item from the history.
    /// </summary>
    CopiedBackAndForth,
}

/// <summary>A short hint, shown in place of the "copied" notification, on how Shapeshifter could have helped.</summary>
public sealed record CoachingTip(CoachingTipKind Kind, string Title, string Message);

/// <summary>
/// Watches copies and pastes for habits that Shapeshifter could save the user from, and suggests how. New users
/// easily forget that the history is there, because it stays out of sight until the paste gesture is held.
/// </summary>
/// <remarks>
/// Thread-safe: <see cref="Engine.ShapeshifterEngine"/> calls it from the capture thread and the effect loop.
/// Applications are compared by name, because window titles change as the user works.
/// </remarks>
public sealed class CopyCoach
{
    /// <summary>How soon after the first copy the user must be back for <see cref="CoachingTipKind.CopiedBackAndForth"/>.</summary>
    public static readonly TimeSpan BackAndForthWindow = TimeSpan.FromSeconds(30);

    private readonly IClipboardSourceLocator? sourceLocator;
    private readonly Func<string> modifierName;
    private readonly Func<bool> isEnabled;
    private readonly TimeProvider time;
    private readonly Lock gate = new();
    private Guid? currentItemId;
    private (string Application, long At)? lastCopy;
    private (string From, string To, long CopiedAt)? pasteElsewhere;
    private long? lastTipAt;

    /// <param name="sourceLocator">Tells which application is in front when the user pastes.</param>
    /// <param name="modifierName">"Ctrl" or "Cmd", as the tips spell the paste gesture.</param>
    /// <param name="isEnabled">Whether tips are wanted; read each time one would be shown.</param>
    /// <param name="time">The clock.</param>
    public CopyCoach(IClipboardSourceLocator? sourceLocator, string modifierName, Func<bool>? isEnabled = null, TimeProvider? time = null)
        : this(sourceLocator, () => modifierName, isEnabled, time)
    {
    }

    /// <param name="sourceLocator">Tells which application is in front when the user pastes.</param>
    /// <param name="modifierName">"Ctrl", "Cmd" or "Win"; read each time a tip is shown, as the user can change the shortcut.</param>
    /// <param name="isEnabled">Whether tips are wanted; read each time one would be shown.</param>
    /// <param name="time">The clock.</param>
    public CopyCoach(IClipboardSourceLocator? sourceLocator, Func<string> modifierName, Func<bool>? isEnabled = null, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(modifierName);
        this.sourceLocator = sourceLocator;
        this.modifierName = modifierName;
        this.isEnabled = isEnabled ?? (() => true);
        this.time = time ?? TimeProvider.System;
    }

    /// <summary>The least time between two tips, so they remind rather than nag.</summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Remembers what is on the clipboard when Shapeshifter starts, without coaching about it.</summary>
    public void OnStartupCapture(ClipboardItem item)
    {
        lock (gate)
        {
            currentItemId = item.Id;
        }
    }

    /// <summary>The user copied something. Returns the tip to show instead of the "copied" notification, if any.</summary>
    /// <param name="item">The item in the history the copy became, or moved to the top.</param>
    /// <param name="isNew">Whether the content was new to the history.</param>
    /// <param name="source">Where this copy came from, if known (an existing item keeps the source of its first copy).</param>
    public CoachingTip? OnCopied(ClipboardItem item, bool isNew, ClipboardSource? source)
    {
        lock (gate)
        {
            var now = time.GetTimestamp();
            var application = Normalize(source?.ApplicationName);
            var wasCurrent = currentItemId == item.Id;
            currentItemId = item.Id;
            var returning = pasteElsewhere;
            pasteElsewhere = null;
            lastCopy = application is null ? null : (application, now);

            CoachingTip? tip = null;
            if (!isNew && !wasCurrent)
            {
                // Copying what is already on the clipboard (a second Ctrl+C to be sure) is no habit Shapeshifter
                // could help with; copying something that was replaced since is.
                tip = new CoachingTip(
                    CoachingTipKind.CopiedAgain,
                    Strings.TipAlreadyInHistoryTitle,
                    Localizer.Format(Strings.TipAlreadyInHistoryMessage, Localizer.ModifierKey(modifierName())));
            }
            else if (returning is { } trip
                && application is not null
                && string.Equals(application, trip.From, StringComparison.OrdinalIgnoreCase)
                && time.GetElapsedTime(trip.CopiedAt, now) <= BackAndForthWindow)
            {
                tip = new CoachingTip(
                    CoachingTipKind.CopiedBackAndForth,
                    Strings.TipDidYouKnowTitle,
                    Localizer.Format(Strings.TipCopyBackAndForthMessage, trip.From, Localizer.ModifierKey(modifierName()), trip.To));
            }

            if (tip is null || !isEnabled() || (lastTipAt is { } shown && time.GetElapsedTime(shown, now) < Cooldown))
            {
                return null;
            }

            lastTipAt = now;
            return tip;
        }
    }

    /// <summary>The user pasted the current clipboard with a plain, quick paste gesture.</summary>
    public void OnPasted()
    {
        // Asked outside the lock: it queries the window system.
        var target = Normalize(LocateTarget()?.ApplicationName);
        lock (gate)
        {
            pasteElsewhere = lastCopy is { } copy
                && target is not null
                && !string.Equals(copy.Application, target, StringComparison.OrdinalIgnoreCase)
                ? (copy.Application, target, copy.At)
                : null;
        }
    }

    /// <summary>
    /// The user opened the picker: they know the history is there, so what they do next is no habit to coach.
    /// </summary>
    public void OnPickerUsed()
    {
        lock (gate)
        {
            lastCopy = null;
            pasteElsewhere = null;
        }
    }

    private ClipboardSource? LocateTarget()
    {
        try
        {
            return sourceLocator?.GetCurrentSource();
        }
        catch (Exception)
        {
            // Locators must not throw; a tip is never worth failing a paste for.
            return null;
        }
    }

    private static string? Normalize(string? application) =>
        string.IsNullOrWhiteSpace(application) ? null : application.Trim();
}
