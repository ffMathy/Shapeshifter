using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using SharpHook.Data;

namespace Shapeshifter.Platform.Input;

/// <summary>Translates libuiohook key codes into gesture keys for one platform and paste shortcut.</summary>
internal sealed class KeyMap
{
    private KeyMap(bool useCommandKey, bool useWindowsKey)
    {
        UsesCommandKey = useCommandKey;
        UsesWindowsKey = useWindowsKey;
    }

    /// <summary>macOS pastes with Command; Windows and Linux with Control.</summary>
    public bool UsesCommandKey { get; }

    /// <summary>
    /// The gesture is Win+V (issue #28): the Windows key is the gesture's modifier, while pasting still sends Ctrl+V.
    /// </summary>
    public bool UsesWindowsKey { get; }

    public string ModifierName => UsesCommandKey ? "Cmd" : UsesWindowsKey ? "Win" : "Ctrl";

    /// <summary>The modifier of the keystroke that pastes, which is not the gesture's modifier with Win+V.</summary>
    public KeyCode PasteModifier => UsesCommandKey ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

    /// <summary>The paste shortcut that is actually used: Win+V only where the platform supports it.</summary>
    public PasteShortcut Shortcut => UsesWindowsKey ? PasteShortcut.WindowsV : PasteShortcut.ControlV;

    public static bool SupportsWindowsKey => OperatingSystem.IsWindows();

    public static KeyMap ForCurrentPlatform(PasteShortcut shortcut = PasteShortcut.ControlV)
    {
        if (OperatingSystem.IsMacOS())
        {
            return ForCommandKey();
        }

        return shortcut == PasteShortcut.WindowsV && SupportsWindowsKey ? ForWindowsKey() : ForControlKey();
    }

    public static KeyMap ForControlKey() => new(false, false);

    public static KeyMap ForCommandKey() => new(true, false);

    public static KeyMap ForWindowsKey() => new(false, true);

    private bool GestureUsesMeta => UsesCommandKey || UsesWindowsKey;

    public GestureKey Map(KeyCode keyCode) => keyCode switch
    {
        KeyCode.VcLeftControl or KeyCode.VcRightControl when !GestureUsesMeta => GestureKey.Modifier,
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta when GestureUsesMeta => GestureKey.Modifier,
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
        KeyCode.VcBackspace when UsesCommandKey => GestureKey.Delete,
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

    private static bool IsControl(KeyCode key) => key is KeyCode.VcLeftControl or KeyCode.VcRightControl;

    private static bool IsMeta(KeyCode key) => key is KeyCode.VcLeftMeta or KeyCode.VcRightMeta;
}
