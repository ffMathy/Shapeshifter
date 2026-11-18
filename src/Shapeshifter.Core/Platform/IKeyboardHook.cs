using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Platform;

/// <summary>The keys that take part in the paste gesture, independent of the platform's key codes.</summary>
public enum GestureKey
{
    Other,

    /// <summary>
    /// Ctrl on Windows and Linux, Command on macOS; with the alternative shortcut, the Windows key on Windows and Ctrl on
    /// macOS.
    /// </summary>
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

    /// <summary>The S key, which starts searching the history.</summary>
    Search,
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

    /// <summary>
    /// A human-readable name of the gesture modifier: "Ctrl", "Cmd" or "Win". Changes with <see cref="PasteShortcut"/>.
    /// </summary>
    string ModifierName { get; }

    /// <summary>
    /// Which keys the gesture uses. Takes effect with the next key press; where there is no alternative
    /// (<see cref="ShortcutModifierNames"/> has one entry), <see cref="Settings.PasteShortcut.Alternative"/> acts as the
    /// standard keys.
    /// </summary>
    PasteShortcut PasteShortcut { get; set; }

    /// <summary>
    /// The modifier names of the shortcuts the user can choose from, in the order of <see cref="Settings.PasteShortcut"/>:
    /// "Ctrl" and "Win" on Windows, "Cmd" and "Ctrl" on macOS, and only "Ctrl" on Linux.
    /// </summary>
    IReadOnlyList<string> ShortcutModifierNames { get; }

    /// <summary>
    /// Whether the gesture's modifier is the modifier of the paste keystroke (Ctrl+V, or Cmd+V on macOS), so the paste
    /// can be sent while the user still holds it. <see langword="false"/> with the alternative shortcuts (Win+V on
    /// Windows, Ctrl+V on macOS).
    /// </summary>
    bool GestureModifierPastes { get; }

    /// <summary>
    /// Some platforms (X11) can only suppress keys that were grabbed beforehand. While a gesture is active the
    /// navigation keys (arrows, Delete, Escape, 1 to 9, Space, Enter and S) are grabbed, with and without the modifier
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

/// <summary>A window of another application that had the keyboard focus, as the platform identifies it.</summary>
/// <param name="Handle">A window handle on Windows and X11, the process id of the application on macOS.</param>
public readonly record struct FocusedWindow(nint Handle);

/// <summary>
/// Finds out which window has the keyboard focus, and gives it back. The picker takes the focus while the user searches
/// the history; afterwards the focus has to return to the window the user was working in, so the paste lands there.
/// </summary>
public interface IFocusedWindowTracker
{
    /// <summary>The window (of another application) that has the keyboard focus, or <see langword="null"/>. Must not throw.</summary>
    FocusedWindow? Capture();

    /// <summary>Gives the keyboard focus to <paramref name="window"/>, and returns once it has it (or did not get it in time).</summary>
    Task<bool> RestoreAsync(FocusedWindow window, CancellationToken cancellationToken = default);
}

/// <summary>Moves the keyboard focus between Shapeshifter's picker and the window the user was working in.</summary>
public interface IWindowFocus : IFocusedWindowTracker
{
    /// <summary>
    /// Gives the keyboard focus to one of Shapeshifter's own windows (its native handle: an HWND, an NSWindow or an X11
    /// window), although another application is active. Called on the UI thread.
    /// </summary>
    void ActivateOwnWindow(nint nativeHandle);
}
