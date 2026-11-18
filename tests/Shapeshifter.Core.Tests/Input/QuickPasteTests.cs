using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

/// <summary>
/// With the standard shortcut, a quick Ctrl+V pastes as soon as V comes up, while Ctrl may still be held, so the user can
/// paste over and over with Ctrl held down, as without Shapeshifter (issue #639). Holding V shows the history.
/// </summary>
public sealed class QuickPasteTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private static GestureHarness Create(int itemCount = 3, TimeSpan? showDelay = null) =>
        GestureHarness.WithItems(itemCount, showDelay: showDelay ?? Delay, pasteOnTriggerRelease: true);

    [Fact]
    public void PasteOnTriggerRelease_IsOnByDefault() =>
        Assert.True(new PasteGestureStateMachine(() => ([], 0)).PasteOnTriggerRelease);

    [Fact]
    public void ReleasingV_BeforeThePickerShows_PastesRightAway_WhileTheModifierIsStillHeld()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        Assert.True(h.Down(GestureKey.Trigger).Suppress);

        var up = h.Up(GestureKey.Trigger);

        Assert.True(up.Suppress);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(up.Effects));
        Assert.False(h.Machine.IsActive);
        Assert.True(h.Machine.IsModifierDown);

        // Releasing the modifier afterwards is an ordinary key release.
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Modifier));
    }

    [Fact]
    public void HoldingTheModifier_EachTapOfV_PastesAgain_AndOtherKeysInBetweenReachTheApplication()
    {
        // Ctrl held: V, Down, V, Down, V, as when pasting into one row after another.
        var h = Create();
        h.Down(GestureKey.Modifier);

        for (var paste = 0; paste < 3; paste++)
        {
            Assert.True(h.Down(GestureKey.Trigger).Suppress);
            Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Trigger).Effects));

            Assert.Equal(GestureResult.PassThrough, h.Down(GestureKey.Down));
            Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Down));
        }

        Assert.Equal(3, h.SessionCount);
        Assert.False(h.Machine.IsActive);
    }

    [Fact]
    public void AutoRepeatOfV_BeforeThePickerShows_IsSwallowed_AndTheReleaseStillPastes()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        var repeat = h.Down(GestureKey.Trigger);

        Assert.Equal(GestureResult.Swallow, repeat);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Trigger).Effects));
    }

    [Fact]
    public void HoldingV_ShowsThePicker_AndReleasingVThenKeepsItOpen_UntilTheModifierIsReleased()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        var tick = h.Advance(Delay);
        Assert.IsType<ShowPicker>(Assert.Single(tick.Effects));

        var up = h.Up(GestureKey.Trigger);
        Assert.Equal(GestureResult.Swallow, up);
        Assert.True(h.Machine.IsActive);
        Assert.True(h.Machine.IsPickerVisible);

        // V again moves to the next item, and its release does not paste either.
        var next = Assert.IsType<UpdatePicker>(Assert.Single(h.Down(GestureKey.Trigger).Effects));
        Assert.Equal(1, next.Selection.SelectedIndex);
        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));

        var release = h.Up(GestureKey.Modifier);
        Assert.Collection(
            release.Effects,
            effect => Assert.IsType<HidePicker>(effect),
            effect => Assert.Equal(new PerformAction(GestureHarness.IdFor(1), 0), effect));
    }

    [Fact]
    public void ReleasingV_JustBeforeTheShowDelay_StillPastes()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        Assert.Empty(h.Advance(Delay - TimeSpan.FromMilliseconds(1)).Effects);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Trigger).Effects));

        // The gesture is over, so the timer shows nothing afterwards.
        Assert.Empty(h.Advance(Delay).Effects);
        Assert.False(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void NavigatingWhileVIsHeld_ShowsThePicker_SoReleasingVNoLongerPastes()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        Assert.IsType<ShowPicker>(Assert.Single(h.Press(GestureKey.Down).Effects));
        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));

        var release = h.Up(GestureKey.Modifier);
        Assert.Equal(new PerformAction(GestureHarness.IdFor(1), 0), release.Effects[^1]);
    }

    [Fact]
    public void SpaceWhileVIsHeld_KeepsThePickerOpen_AndReleasingVDoesNotPaste()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        h.Press(GestureKey.Space);

        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Modifier));
        Assert.True(h.Machine.IsHeldOpen);
        Assert.True(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void EscapeWhileVIsHeld_Cancels_AndReleasingVDoesNotPaste()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        Assert.IsType<GestureCancelled>(Assert.Single(h.Down(GestureKey.Escape).Effects));

        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
    }

    [Fact]
    public void ReleasingTheModifierFirst_PastesThen_AndReleasingVAfterwardsDoesNothing()
    {
        var h = Create();
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
    }

    [Fact]
    public void WithAnEmptyHistory_ReleasingV_Pastes()
    {
        var h = Create(itemCount: 0);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Trigger).Effects));
    }

    [Fact]
    public void WithoutAShowDelay_ThePickerShowsAtOnce_SoReleasingVDoesNotPaste()
    {
        var h = Create(showDelay: TimeSpan.Zero);
        h.Down(GestureKey.Modifier);
        Assert.IsType<ShowPicker>(Assert.Single(h.Down(GestureKey.Trigger).Effects));

        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
        Assert.True(h.Machine.IsActive);
    }

    [Fact]
    public void WhenTurnedOff_ReleasingVKeepsTheGestureGoing_AndTheModifierStillShowsTheHistory()
    {
        // The alternative shortcuts (Win+V, Ctrl+V on macOS) cannot paste while their modifier is held.
        var h = GestureHarness.WithItems(3, showDelay: Delay, pasteOnTriggerRelease: false);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
        Assert.True(h.Machine.IsActive);
        Assert.IsType<ShowPicker>(Assert.Single(h.Advance(Delay).Effects));
    }
}
