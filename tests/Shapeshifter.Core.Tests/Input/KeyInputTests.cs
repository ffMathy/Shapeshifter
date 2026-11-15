using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

public sealed class KeyInputTests
{
    [Fact]
    public void DefaultGestureKey_IsOther()
    {
        Assert.Equal(GestureKey.Other, default(GestureKey));
        Assert.Equal(new KeyInput(GestureKey.Other, false), default(KeyInput));
    }

    [Fact]
    public void KeyInput_HasValueEquality()
    {
        Assert.Equal(new KeyInput(GestureKey.Trigger, true), new KeyInput(GestureKey.Trigger, true));
        Assert.NotEqual(new KeyInput(GestureKey.Trigger, true), new KeyInput(GestureKey.Trigger, false));
        Assert.NotEqual(new KeyInput(GestureKey.Trigger, true), new KeyInput(GestureKey.Modifier, true));
    }

    [Fact]
    public void GestureKey_DefinesAllGestureKeys()
    {
        Assert.Equal(
            ["Other", "Modifier", "Trigger", "Up", "Down", "Left", "Right", "Delete", "Escape", "Shift", "Alt", "OtherModifier"],
            Enum.GetNames<GestureKey>());
    }
}
