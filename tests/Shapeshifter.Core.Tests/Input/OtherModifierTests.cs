using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

/// <summary>Shortcuts that combine V with more than the paste modifier belong to the focused application.</summary>
public sealed class OtherModifierTests
{
    [Theory]
    [InlineData(GestureKey.Shift)]
    [InlineData(GestureKey.Alt)]
    [InlineData(GestureKey.OtherModifier)]
    public void VWithAnotherModifierHeld_PassesThroughAndStartsNoGesture(GestureKey extra)
    {
        var harness = GestureHarness.WithItems(3);
        harness.Down(GestureKey.Modifier);
        harness.Down(extra);

        var down = harness.Down(GestureKey.Trigger);
        var up = harness.Up(GestureKey.Trigger);

        Assert.False(down.Suppress);
        Assert.False(up.Suppress);
        Assert.Empty(down.Effects);
        Assert.False(harness.Machine.IsActive);
        Assert.Equal(0, harness.SessionCount);
    }

    [Fact]
    public void AltGr_ReportedAsCtrlPlusAlt_PassesThrough()
    {
        // Windows reports AltGr as a synthetic left Ctrl followed by right Alt.
        var harness = GestureHarness.WithItems(2);
        harness.Down(GestureKey.Modifier);
        harness.Down(GestureKey.Alt);

        Assert.False(harness.Down(GestureKey.Trigger).Suppress);
        Assert.False(harness.Machine.IsActive);
    }

    [Fact]
    public void ReleasingTheOtherModifier_MakesTheGestureAvailableAgain()
    {
        var harness = GestureHarness.WithItems(2);
        harness.Down(GestureKey.Modifier);
        harness.Down(GestureKey.Shift);
        harness.Press(GestureKey.Trigger);
        harness.Up(GestureKey.Shift);

        Assert.True(harness.Down(GestureKey.Trigger).Suppress);
        Assert.True(harness.Machine.IsActive);
    }

    [Fact]
    public void PressingShiftDuringAnActiveGesture_DoesNotStopCycling()
    {
        var harness = GestureHarness.WithItems(3);
        harness.Start();
        harness.Down(GestureKey.Shift);

        var next = harness.Down(GestureKey.Trigger);

        Assert.True(next.Suppress);
        Assert.Equal(1, harness.Machine.CurrentSelection!.SelectedIndex);
    }

    [Theory]
    [InlineData(GestureKey.Shift)]
    [InlineData(GestureKey.Alt)]
    [InlineData(GestureKey.OtherModifier)]
    public void ModifierKeysThemselves_AreNeverSuppressed(GestureKey key)
    {
        var harness = GestureHarness.WithItems(1);
        harness.Start();

        Assert.False(harness.Down(key).Suppress);
        Assert.False(harness.Up(key).Suppress);
    }
}
