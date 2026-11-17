using Shapeshifter.Core.Platform;
using SharpHook.Data;

namespace Shapeshifter.Platform.Input;

/// <summary>Translates libuiohook key codes into gesture keys for one platform.</summary>
internal sealed class KeyMap
{
    private KeyMap(bool useCommandKey)
    {
        UsesCommandKey = useCommandKey;
    }

    /// <summary>macOS pastes with Command; Windows and Linux with Control.</summary>
    public bool UsesCommandKey { get; }

    public string ModifierName => UsesCommandKey ? "Cmd" : "Ctrl";

    public KeyCode PasteModifier => UsesCommandKey ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

    public static KeyMap ForCurrentPlatform() => new(OperatingSystem.IsMacOS());

    public static KeyMap ForControlKey() => new(false);

    public static KeyMap ForCommandKey() => new(true);

    public GestureKey Map(KeyCode keyCode) => keyCode switch
    {
        KeyCode.VcLeftControl or KeyCode.VcRightControl when !UsesCommandKey => GestureKey.Modifier,
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta when UsesCommandKey => GestureKey.Modifier,
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

    /// <summary>Left and right variants of the modifier are interchangeable when matching our own echo.</summary>
    public bool IsSameKey(KeyCode expected, KeyCode actual) =>
        expected == actual || (Map(expected) == GestureKey.Modifier && Map(actual) == GestureKey.Modifier);
}
