using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using SharpHook.Data;

namespace Shapeshifter.Platform.Input;

/// <summary>Translates libuiohook key codes into gesture keys for one platform and paste shortcut.</summary>
internal sealed class KeyMap
{
    private KeyMap(bool pastesWithCommandKey, bool gestureUsesMetaKey)
    {
        PastesWithCommandKey = pastesWithCommandKey;
        GestureUsesMetaKey = gestureUsesMetaKey;
    }

    /// <summary>macOS pastes with Command; Windows and Linux with Control.</summary>
    public bool PastesWithCommandKey { get; }

    /// <summary>
    /// Whether the gesture's modifier is the Command key (macOS) or the Windows key, rather than Control. With Win+V on
    /// Windows and Ctrl+V on macOS (issue #28), the gesture's modifier differs from the one that pastes.
    /// </summary>
    public bool GestureUsesMetaKey { get; }

    /// <summary>The gesture is Win+V: Windows opens the Start menu when the Windows key is released on its own.</summary>
    public bool UsesWindowsKey => GestureUsesMetaKey && !PastesWithCommandKey;

    public string ModifierName => ModifierNameOf(PastesWithCommandKey, GestureUsesMetaKey);

    /// <summary>The modifier of the keystroke that pastes, which is not the gesture's modifier with the alternative shortcut.</summary>
    public KeyCode PasteModifier => PastesWithCommandKey ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

    /// <summary>Whether the gesture's modifier is the paste keystroke's modifier (Ctrl+V, or Cmd+V on macOS).</summary>
    public bool GestureModifierPastes => GestureUsesMetaKey == PastesWithCommandKey;

    /// <summary>The paste shortcut that is actually used.</summary>
    public PasteShortcut Shortcut => GestureModifierPastes ? PasteShortcut.Standard : PasteShortcut.Alternative;

    /// <summary>Win+V on Windows and Ctrl+V on macOS; X11 has no alternative (Super+V is the desktop's own).</summary>
    public static bool SupportsAlternative => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    /// <summary>The modifier names of the standard and the alternative shortcut on this platform.</summary>
    public static IReadOnlyList<string> ShortcutModifierNames =>
        OperatingSystem.IsMacOS() ? ["Cmd", "Ctrl"] : OperatingSystem.IsWindows() ? ["Ctrl", "Win"] : ["Ctrl"];

    public static KeyMap ForCurrentPlatform(PasteShortcut shortcut = PasteShortcut.Standard)
    {
        var mac = OperatingSystem.IsMacOS();
        var alternative = shortcut == PasteShortcut.Alternative && SupportsAlternative;
        return new KeyMap(mac, gestureUsesMetaKey: mac != alternative);
    }

    public static KeyMap ForControlKey() => new(false, false);

    public static KeyMap ForCommandKey() => new(true, true);

    public static KeyMap ForWindowsKey() => new(false, true);

    /// <summary>Ctrl+V on macOS, where pasting still sends Cmd+V.</summary>
    public static KeyMap ForControlKeyOnMac() => new(true, false);

    private static string ModifierNameOf(bool mac, bool meta) => meta ? (mac ? "Cmd" : "Win") : "Ctrl";

    public GestureKey Map(KeyCode keyCode) => keyCode switch
    {
        KeyCode.VcLeftControl or KeyCode.VcRightControl when !GestureUsesMetaKey => GestureKey.Modifier,
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta when GestureUsesMetaKey => GestureKey.Modifier,
        KeyCode.VcLeftShift or KeyCode.VcRightShift => GestureKey.Shift,
        KeyCode.VcLeftAlt or KeyCode.VcRightAlt => GestureKey.Alt,
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta => GestureKey.OtherModifier,
        KeyCode.VcLeftControl or KeyCode.VcRightControl => GestureKey.OtherModifier,
        KeyCode.VcV => GestureKey.Trigger,
        KeyCode.VcUp => GestureKey.Up,
        KeyCode.VcDown => GestureKey.Down,
        KeyCode.VcLeft => GestureKey.Left,
        KeyCode.VcRight => GestureKey.Right,
        KeyCode.VcDelete => GestureKey.Delete,

        // The key labelled "delete" on Mac keyboards is Backspace.
        KeyCode.VcBackspace when PastesWithCommandKey => GestureKey.Delete,
        KeyCode.VcEscape => GestureKey.Escape,
        KeyCode.VcS => GestureKey.Search,
        KeyCode.Vc1 or KeyCode.VcNumPad1 => GestureKey.Digit1,
        KeyCode.Vc2 or KeyCode.VcNumPad2 => GestureKey.Digit2,
        KeyCode.Vc3 or KeyCode.VcNumPad3 => GestureKey.Digit3,
        KeyCode.Vc4 or KeyCode.VcNumPad4 => GestureKey.Digit4,
        KeyCode.Vc5 or KeyCode.VcNumPad5 => GestureKey.Digit5,
        KeyCode.Vc6 or KeyCode.VcNumPad6 => GestureKey.Digit6,
        KeyCode.Vc7 or KeyCode.VcNumPad7 => GestureKey.Digit7,
        KeyCode.Vc8 or KeyCode.VcNumPad8 => GestureKey.Digit8,
        KeyCode.Vc9 or KeyCode.VcNumPad9 => GestureKey.Digit9,
        KeyCode.VcSpace => GestureKey.Space,
        KeyCode.VcEnter or KeyCode.VcNumPadEnter => GestureKey.Enter,
        _ => GestureKey.Other,
    };

    /// <summary>Left and right variants of a modifier are interchangeable when matching our own echo.</summary>
    public static bool IsSameKey(KeyCode expected, KeyCode actual) =>
        expected == actual
        || (IsControl(expected) && IsControl(actual))
        || (IsMeta(expected) && IsMeta(actual));

    /// <summary>Whether <paramref name="key"/> is either variant of the paste keystroke's modifier.</summary>
    public bool IsPasteModifier(KeyCode key) => PastesWithCommandKey ? IsMeta(key) : IsControl(key);

    private static bool IsControl(KeyCode key) => key is KeyCode.VcLeftControl or KeyCode.VcRightControl;

    private static bool IsMeta(KeyCode key) => key is KeyCode.VcLeftMeta or KeyCode.VcRightMeta;
}
