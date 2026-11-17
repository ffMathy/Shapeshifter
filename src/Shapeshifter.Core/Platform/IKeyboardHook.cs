namespace Shapeshifter.Core.Platform;

/// <summary>The keys that take part in the paste gesture, independent of the platform's key codes.</summary>
public enum GestureKey
{
    Other,

    /// <summary>Ctrl on Windows and Linux, Command on macOS.</summary>
    Modifier,

    /// <summary>The V key.</summary>
    Trigger,
    Up,
    Down,
    Left,
    Right,
    Delete,
    Escape,

    /// <summary>Either Shift key.</summary>
    Shift,

    /// <summary>Alt / Option, including AltGr.</summary>
    Alt,

    /// <summary>The platform's remaining modifier: the Windows/Super key, or Ctrl on macOS.</summary>
    OtherModifier,

    /// <summary>The digit keys 1 to 9 (top row or keypad), which jump to the item with that number.</summary>
    Digit1,
    Digit2,
    Digit3,
    Digit4,
    Digit5,
    Digit6,
    Digit7,
    Digit8,
    Digit9,

    /// <summary>The space bar, which keeps the picker open after the modifier is released (issue #420).</summary>
    Space,

    /// <summary>Enter (or the keypad's Enter), which pastes the selected item right away.</summary>
    Enter,
}

public readonly record struct KeyInput(GestureKey Key, bool IsDown);

/// <summary>
/// A system-wide keyboard hook. <see cref="KeyHandler"/> is invoked synchronously on the hook thread for every
/// key event that was not produced by this application; returning <see langword="true"/> asks the platform to
/// suppress the event so that the focused application never receives it.
/// </summary>
public interface IKeyboardHook : IDisposable
{
    Func<KeyInput, bool>? KeyHandler { get; set; }

    /// <summary>A human-readable name of the gesture modifier on this platform ("Ctrl" or "Cmd").</summary>
    string ModifierName { get; }

    /// <summary>
    /// Some platforms (X11) can only suppress keys that were grabbed beforehand. While a gesture is active the
    /// navigation keys (arrows, Delete, Escape, 1 to 9, Space and Enter) are grabbed, with and without the modifier
    /// (the picker can be kept open after the modifier is released), so the focused application does not see them.
    /// </summary>
    void SetNavigationCaptureEnabled(bool enabled);

    /// <summary>Starts the hook. Throws <see cref="PlatformNotSupportedException"/> when hooks are unavailable.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);
}

/// <summary>Sends the platform's paste keystroke (Ctrl+V or Cmd+V) to the focused application.</summary>
public interface IPasteSimulator
{
    Task SimulatePasteAsync(CancellationToken cancellationToken = default);
}
