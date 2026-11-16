using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

public sealed class PasteGestureStateMachineTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private static Guid Id(int index) => GestureHarness.IdFor(index);

    private static PickerSelection AssertShow(GestureEffect effect)
    {
        var show = Assert.IsType<ShowPicker>(effect);
        return show.Selection;
    }

    private static PickerSelection AssertUpdate(GestureEffect effect)
    {
        var update = Assert.IsType<UpdatePicker>(effect);
        return update.Selection;
    }

    private static PickerSelection SingleShow(GestureResult result) => AssertShow(Assert.Single(result.Effects));

    private static PickerSelection SingleUpdate(GestureResult result) => AssertUpdate(Assert.Single(result.Effects));

    private static void AssertSelection(PickerSelection selection, int index, int actionIndex = 0, PickerPane pane = PickerPane.Items)
    {
        Assert.Equal(index, selection.SelectedIndex);
        Assert.Equal(actionIndex, selection.SelectedActionIndex);
        Assert.Equal(pane, selection.Pane);
        Assert.Equal(selection.ItemIds[index], selection.SelectedItemId);
    }

    private static void AssertIdle(PasteGestureStateMachine machine)
    {
        Assert.False(machine.IsActive);
        Assert.False(machine.IsPickerVisible);
        Assert.Null(machine.CurrentSelection);
    }

    // ---- construction and defaults ----

    [Fact]
    public void Constructor_NullFactory_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new PasteGestureStateMachine(null!));

    [Fact]
    public void NewMachine_IsIdle_WithDefaultShowDelayOf300ms()
    {
        var machine = new PasteGestureStateMachine(() => ([], 0));

        AssertIdle(machine);
        Assert.False(machine.IsModifierDown);
        Assert.Equal(TimeSpan.FromMilliseconds(300), machine.ShowDelay);
    }

    [Fact]
    public void SessionFactory_IsNotCalledUntilTheGestureStarts()
    {
        var h = GestureHarness.WithItems(3);

        h.Down(GestureKey.Modifier);
        h.Press(GestureKey.Up);
        h.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(0, h.SessionCount);

        h.Down(GestureKey.Trigger);
        Assert.Equal(1, h.SessionCount);

        // Further V presses in the same gesture do not re-query the history.
        h.Up(GestureKey.Trigger);
        h.Press(GestureKey.Trigger);
        Assert.Equal(1, h.SessionCount);
    }

    [Fact]
    public void SessionFactory_IsCalledOncePerGesture()
    {
        var h = GestureHarness.WithItems(3);

        h.Start();
        h.Up(GestureKey.Modifier);
        h.Start();
        h.Up(GestureKey.Modifier);

        Assert.Equal(2, h.SessionCount);
    }

    [Fact]
    public void Session_IsSnapshotted_LaterHistoryChangesDoNotAffectTheGesture()
    {
        var h = GestureHarness.WithItems(2);
        h.StartAndShow();

        h.Items.Clear();
        h.Items.AddRange(GestureHarness.CreateItems(5));

        var selection = SingleUpdate(h.Down(GestureKey.Down));
        Assert.Equal([Id(0), Id(1)], selection.ItemIds);
        AssertSelection(selection, 1);
    }

    // ---- modifier tracking ----

    [Fact]
    public void Modifier_DownAndUp_AlwaysPassThrough_AndAreTracked()
    {
        var h = GestureHarness.WithItems(3);

        var down = h.Down(GestureKey.Modifier);
        Assert.False(down.Suppress);
        Assert.Empty(down.Effects);
        Assert.True(h.Machine.IsModifierDown);

        var up = h.Up(GestureKey.Modifier);
        Assert.False(up.Suppress);
        Assert.Empty(up.Effects);
        Assert.False(h.Machine.IsModifierDown);
    }

    [Fact]
    public void ModifierUp_EndingAGesture_IsNotSuppressed()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        var result = h.Up(GestureKey.Modifier);

        Assert.False(result.Suppress);
        Assert.False(h.Machine.IsModifierDown);
    }

    // ---- trigger without modifier ----

    [Fact]
    public void Trigger_WithoutModifier_PassesThrough_AndDoesNotStartAGesture()
    {
        var h = GestureHarness.WithItems(3);

        var down = h.Down(GestureKey.Trigger);
        var up = h.Up(GestureKey.Trigger);

        Assert.Equal(GestureResult.PassThrough, down);
        Assert.Equal(GestureResult.PassThrough, up);
        Assert.False(h.Machine.IsActive);
        Assert.Equal(0, h.SessionCount);
    }

    [Fact]
    public void Trigger_AfterModifierReleased_PassesThrough()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Up(GestureKey.Modifier);

        var down = h.Down(GestureKey.Trigger);

        Assert.False(down.Suppress);
        Assert.False(h.Machine.IsActive);
    }

    // ---- quick tap ----

    [Fact]
    public void QuickTap_IsSuppressed_AndPastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);

        var vDown = h.Down(GestureKey.Trigger);
        Assert.True(vDown.Suppress);
        Assert.Empty(vDown.Effects);
        Assert.True(h.Machine.IsActive);
        Assert.False(h.Machine.IsPickerVisible);

        var vUp = h.Up(GestureKey.Trigger);
        Assert.True(vUp.Suppress);
        Assert.Empty(vUp.Effects);

        h.Now += TimeSpan.FromMilliseconds(50);
        var release = h.Up(GestureKey.Modifier);

        Assert.False(release.Suppress);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(release.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void QuickTap_ModifierReleasedWhileTriggerStillHeld_PastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        var release = h.Up(GestureKey.Modifier);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(release.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void QuickTap_WithEmptyHistory_PastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(0);

        Assert.True(h.Start().Suppress);
        Assert.True(h.Machine.IsActive);
        Assert.Null(h.Machine.CurrentSelection);

        var release = h.Up(GestureKey.Modifier);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(release.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void ActiveGestureBeforeShowDelay_ExposesCurrentSelection_AtDefaultIndex()
    {
        var h = GestureHarness.WithItems(3, defaultIndex: 1);
        h.Start();

        var selection = Assert.IsType<PickerSelection>(h.Machine.CurrentSelection);
        Assert.Equal([Id(0), Id(1), Id(2)], selection.ItemIds);
        AssertSelection(selection, 1);
        Assert.False(h.Machine.IsPickerVisible);
    }

    // ---- show delay and Tick ----

    [Fact]
    public void Tick_BeforeShowDelay_DoesNothing()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Advance(Delay - TimeSpan.FromTicks(1));

        Assert.Equal(GestureResult.PassThrough, result);
        Assert.False(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void Tick_ExactlyAtShowDelay_ShowsPickerAtDefaultIndex()
    {
        var h = GestureHarness.WithItems(3, defaultIndex: 0);
        h.Start();

        var result = h.Advance(Delay);

        Assert.False(result.Suppress);
        var selection = SingleShow(result);
        Assert.Equal([Id(0), Id(1), Id(2)], selection.ItemIds);
        AssertSelection(selection, 0);
        Assert.True(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void Tick_AfterPickerShown_DoesNotShowAgain()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        Assert.Empty(h.Advance(TimeSpan.FromSeconds(1)).Effects);
        Assert.Empty(h.Advance(TimeSpan.FromSeconds(10)).Effects);
        Assert.True(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void Tick_WhenIdle_DoesNothing()
    {
        var h = GestureHarness.WithItems(3);

        Assert.Equal(GestureResult.PassThrough, h.Advance(TimeSpan.FromSeconds(5)));

        h.Down(GestureKey.Modifier);
        Assert.Equal(GestureResult.PassThrough, h.Advance(TimeSpan.FromSeconds(5)));
        Assert.False(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void Tick_WithEmptyHistory_NeverShowsPicker()
    {
        var h = GestureHarness.WithItems(0);
        h.Start();

        Assert.Empty(h.Advance(TimeSpan.FromSeconds(5)).Effects);
        Assert.False(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void ShowDelay_IsMeasuredFromTheFirstTriggerPress()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Advance(TimeSpan.FromSeconds(2)); // holding the modifier alone does not count
        h.Down(GestureKey.Trigger);

        Assert.Empty(h.Advance(Delay - TimeSpan.FromMilliseconds(1)).Effects);
        SingleShow(h.Advance(TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public void ShowDelay_ChangedDuringGesture_IsHonouredByTick()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();
        h.Advance(TimeSpan.FromMilliseconds(100));
        Assert.False(h.Machine.IsPickerVisible);

        h.Machine.ShowDelay = TimeSpan.FromMilliseconds(50);
        SingleShow(h.Machine.Tick(h.Now));
    }

    [Fact]
    public void ShowAfterDelay_ThenReleaseWithoutChanges_HidesAndPastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        var release = h.Up(GestureKey.Modifier);

        Assert.False(release.Suppress);
        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<PasteCurrentClipboard>(e));
        AssertIdle(h.Machine);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShowDelayZeroOrNegative_ShowsPickerImmediatelyOnFirstTrigger(int delayMs)
    {
        var h = GestureHarness.WithItems(3, showDelay: TimeSpan.FromMilliseconds(delayMs));
        h.Down(GestureKey.Modifier);

        var result = h.Down(GestureKey.Trigger);

        Assert.True(result.Suppress);
        AssertSelection(SingleShow(result), 0);
        Assert.True(h.Machine.IsPickerVisible);

        // A later tick must not show it a second time.
        Assert.Empty(h.Advance(TimeSpan.FromSeconds(1)).Effects);
    }

    [Fact]
    public void ShowDelayZero_WithEmptyHistory_DoesNotShowPicker()
    {
        var h = GestureHarness.WithItems(0, showDelay: TimeSpan.Zero);
        h.Down(GestureKey.Modifier);

        var result = h.Down(GestureKey.Trigger);

        Assert.True(result.Suppress);
        Assert.Empty(result.Effects);
        Assert.False(h.Machine.IsPickerVisible);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void ShowDelayZero_ReleaseWithoutChanges_PastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(3, showDelay: TimeSpan.Zero);
        h.Start();

        var release = h.Up(GestureKey.Modifier);

        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<PasteCurrentClipboard>(e));
    }

    // ---- repeated trigger presses ----

    [Fact]
    public void SecondTrigger_BeforeShowDelay_SelectsNextItemAndShowsPickerImmediately()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Down(GestureKey.Trigger);

        Assert.True(result.Suppress);
        AssertSelection(SingleShow(result), 1);
        Assert.True(h.Machine.IsPickerVisible);
        Assert.Empty(h.Advance(Delay * 2).Effects);
    }

    [Fact]
    public void FurtherTriggers_WhilePickerVisible_UpdateSelection()
    {
        var h = GestureHarness.WithItems(4);
        h.StartAndShow();

        AssertSelection(SingleUpdate(h.Press(GestureKey.Trigger)), 1);
        AssertSelection(SingleUpdate(h.Press(GestureKey.Trigger)), 2);
        AssertSelection(SingleUpdate(h.Press(GestureKey.Trigger)), 3);
    }

    [Theory]
    [InlineData(1, 0, 1, 0)]
    [InlineData(3, 0, 1, 1)]
    [InlineData(3, 0, 2, 2)]
    [InlineData(3, 0, 3, 0)]
    [InlineData(3, 0, 4, 1)]
    [InlineData(3, 0, 7, 1)]
    [InlineData(5, 2, 1, 3)]
    [InlineData(5, 2, 3, 0)]
    [InlineData(5, 4, 1, 0)]
    public void ExtraTriggerPresses_SelectOlderItems_WithWrapAround(int itemCount, int defaultIndex, int extraPresses, int expectedIndex)
    {
        var h = GestureHarness.WithItems(itemCount, defaultIndex: defaultIndex);
        h.Start();

        for (var i = 0; i < extraPresses; i++)
        {
            Assert.True(h.Press(GestureKey.Trigger).Suppress);
        }

        AssertSelection(Assert.IsType<PickerSelection>(h.Machine.CurrentSelection), expectedIndex);
    }

    [Fact]
    public void AutoRepeatedTriggerDown_IsSuppressed_AndDoesNotAdvance()
    {
        var h = GestureHarness.WithItems(5);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        for (var i = 0; i < 10; i++)
        {
            var repeat = h.Down(GestureKey.Trigger);
            Assert.True(repeat.Suppress);
            Assert.Empty(repeat.Effects);
        }

        AssertSelection(Assert.IsType<PickerSelection>(h.Machine.CurrentSelection), 0);
        Assert.False(h.Machine.IsPickerVisible);
        Assert.Equal(1, h.SessionCount);

        // Releasing and pressing again advances exactly once.
        h.Up(GestureKey.Trigger);
        h.Down(GestureKey.Trigger);
        h.Down(GestureKey.Trigger);
        AssertSelection(Assert.IsType<PickerSelection>(h.Machine.CurrentSelection), 1);
    }

    [Fact]
    public void AutoRepeatDoesNotCountAsQuickTapChange_ReleasePastesCurrentClipboard()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        h.Down(GestureKey.Trigger);
        h.Down(GestureKey.Trigger);

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void ExtraTrigger_WithEmptyHistory_IsSuppressedWithoutEffects()
    {
        var h = GestureHarness.WithItems(0);
        h.Start();

        var result = h.Press(GestureKey.Trigger);

        Assert.True(result.Suppress);
        Assert.Empty(result.Effects);
        Assert.False(h.Machine.IsPickerVisible);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void ExtraTrigger_SwitchesBackToItemsPane_AndResetsActionSelection()
    {
        var h = GestureHarness.WithItems(3, actionCount: 4);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);
        AssertSelection(h.Machine.CurrentSelection!, 0, actionIndex: 1, pane: PickerPane.Actions);

        var selection = SingleUpdate(h.Press(GestureKey.Trigger));

        AssertSelection(selection, 1, actionIndex: 0, pane: PickerPane.Items);
    }

    [Fact]
    public void SecondTrigger_ThenRelease_PerformsDefaultActionOnSelectedItem()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();
        h.Press(GestureKey.Trigger);

        var release = h.Up(GestureKey.Modifier);

        Assert.False(release.Suppress);
        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.Equal(new PerformAction(Id(1), 0), e));
        AssertIdle(h.Machine);
    }

    /// <summary>
    /// Regression test for issue #573: the default (first) action must be selected when the picker appears and
    /// whenever another item is selected, and releasing the modifier must perform it.
    /// </summary>
    [Fact]
    public void DefaultAction_IsSelectedWhenPickerShows_AndAfterChangingItem_AndIsPerformedOnRelease()
    {
        var h = GestureHarness.WithItems(3, actionCount: 4);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        h.Up(GestureKey.Trigger);

        AssertSelection(SingleShow(h.Advance(Delay)), 0, actionIndex: 0);
        AssertSelection(SingleUpdate(h.Press(GestureKey.Down)), 1, actionIndex: 0);
        AssertSelection(SingleUpdate(h.Press(GestureKey.Trigger)), 2, actionIndex: 0);

        Assert.Equal(new PerformAction(Id(2), 0), h.Up(GestureKey.Modifier).Effects[^1]);
    }

    [Fact]
    public void TriggerWrapsBackToDefault_ThenRelease_StillPerformsActionBecauseSelectionChanged()
    {
        var h = GestureHarness.WithItems(2);
        h.Start();
        h.Press(GestureKey.Trigger);
        h.Press(GestureKey.Trigger);
        AssertSelection(h.Machine.CurrentSelection!, 0);

        var release = h.Up(GestureKey.Modifier);

        Assert.Equal(new PerformAction(Id(0), 0), release.Effects[^1]);
    }

    // ---- trigger-up suppression ----

    [Fact]
    public void TriggerUp_WhileGestureActive_IsSuppressed()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);

        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));

        // A stray V-up (no matching down) during an active gesture is also swallowed.
        Assert.Equal(GestureResult.Swallow, h.Up(GestureKey.Trigger));
    }

    [Fact]
    public void TriggerUp_WhenIdle_PassesThrough()
    {
        var h = GestureHarness.WithItems(3);

        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Trigger));

        h.Down(GestureKey.Modifier);
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Trigger));
    }

    /// <summary>
    /// The V key-down was suppressed, so its matching key-up must be suppressed too, even when the user lets go of
    /// the modifier before V (OnTriggerUp's <c>wasDown</c> check exists for exactly this case).
    /// </summary>
    [Fact]
    public void TriggerUp_AfterModifierReleasedFirst_IsSuppressedBecauseItsKeyDownWas()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        Assert.True(h.Down(GestureKey.Trigger).Suppress);
        h.Up(GestureKey.Modifier);

        Assert.True(h.Up(GestureKey.Trigger).Suppress);
    }

    /// <summary>
    /// Regression test for issue #527: releasing Ctrl before V used to emit a stream of Vs, because the auto-repeat
    /// key-downs of the still-held V reached the focused application once the gesture had ended.
    /// </summary>
    [Fact]
    public void AutoRepeatedTrigger_AfterModifierReleasedFirst_IsSuppressed_AndStartsNoNewGesture()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        Assert.True(h.Down(GestureKey.Trigger).Suppress);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));

        for (var i = 0; i < 20; i++)
        {
            var repeat = h.Down(GestureKey.Trigger);
            Assert.True(repeat.Suppress);
            Assert.Empty(repeat.Effects);
            Assert.False(h.Machine.IsActive);
        }

        Assert.True(h.Up(GestureKey.Trigger).Suppress);
        Assert.Equal(1, h.SessionCount);

        // Once V has been released, a plain V press types normally again.
        Assert.Equal(GestureResult.PassThrough, h.Down(GestureKey.Trigger));
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Trigger));
    }

    /// <summary>
    /// After Escape the user is still holding Ctrl+V; the OS keeps sending auto-repeat V key-downs. Those are
    /// auto-repeats of a key that is still held and must be ignored, not start a brand-new gesture.
    /// </summary>
    [Fact]
    public void AutoRepeatedTrigger_AfterEscapeWhileVStillHeld_DoesNotStartANewGesture()
    {
        var h = GestureHarness.WithItems(3);
        h.Down(GestureKey.Modifier);
        h.Down(GestureKey.Trigger);
        h.Advance(Delay);
        h.Press(GestureKey.Escape);
        Assert.False(h.Machine.IsActive);

        var repeat = h.Down(GestureKey.Trigger);

        Assert.True(repeat.Suppress);
        Assert.Empty(repeat.Effects);
        Assert.False(h.Machine.IsActive);
        Assert.Equal(1, h.SessionCount);
    }

    // ---- navigation keys when idle ----

    public static TheoryData<GestureKey> NavigationKeys =>
        [GestureKey.Up, GestureKey.Down, GestureKey.Right, GestureKey.Left, GestureKey.Delete, GestureKey.Escape];

    public static TheoryData<GestureKey> MovementKeys =>
        [GestureKey.Up, GestureKey.Down, GestureKey.Right, GestureKey.Left, GestureKey.Delete];

    [Theory]
    [MemberData(nameof(NavigationKeys))]
    public void NavigationKeys_WhenIdle_PassThrough(GestureKey key)
    {
        var h = GestureHarness.WithItems(3);

        Assert.Equal(GestureResult.PassThrough, h.Down(key));
        Assert.Equal(GestureResult.PassThrough, h.Up(key));

        // Holding the modifier alone (Ctrl+Arrow etc.) does not capture them either.
        h.Down(GestureKey.Modifier);
        Assert.Equal(GestureResult.PassThrough, h.Down(key));
        Assert.Equal(GestureResult.PassThrough, h.Up(key));
        Assert.False(h.Machine.IsActive);
    }

    [Theory]
    [MemberData(nameof(NavigationKeys))]
    public void NavigationKeyUp_WhileActive_IsSwallowedWithoutEffects(GestureKey key)
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        Assert.Equal(GestureResult.Swallow, h.Up(key));
        Assert.True(h.Machine.IsActive);
        AssertSelection(h.Machine.CurrentSelection!, 0);
    }

    [Theory]
    [MemberData(nameof(MovementKeys))]
    public void MovementKeys_WithEmptyHistory_AreSwallowedWithoutEffects(GestureKey key)
    {
        var h = GestureHarness.WithItems(0);
        h.Start();

        Assert.Equal(GestureResult.Swallow, h.Down(key));
        Assert.True(h.Machine.IsActive);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void OtherKeys_AlwaysPassThrough()
    {
        var h = GestureHarness.WithItems(3);
        Assert.Equal(GestureResult.PassThrough, h.Down(GestureKey.Other));

        h.StartAndShow();
        Assert.Equal(GestureResult.PassThrough, h.Down(GestureKey.Other));
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Other));
        Assert.True(h.Machine.IsActive);
        AssertSelection(h.Machine.CurrentSelection!, 0);
    }

    [Fact]
    public void UnknownEnumValue_PassesThrough()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        Assert.Equal(GestureResult.PassThrough, h.Down((GestureKey)999));
        Assert.True(h.Machine.IsActive);
    }

    // ---- Up / Down in the items pane ----

    [Theory]
    [InlineData(4, 0, GestureKey.Down, 1)]
    [InlineData(4, 2, GestureKey.Down, 3)]
    [InlineData(4, 3, GestureKey.Down, 0)]
    [InlineData(4, 1, GestureKey.Up, 0)]
    [InlineData(4, 0, GestureKey.Up, 3)]
    [InlineData(1, 0, GestureKey.Up, 0)]
    [InlineData(1, 0, GestureKey.Down, 0)]
    public void UpDown_MoveItemSelection_WithWrapAround(int itemCount, int defaultIndex, GestureKey key, int expectedIndex)
    {
        var h = GestureHarness.WithItems(itemCount, defaultIndex: defaultIndex);
        h.StartAndShow();

        var result = h.Down(key);

        Assert.True(result.Suppress);
        AssertSelection(SingleUpdate(result), expectedIndex);
    }

    [Fact]
    public void Arrow_BeforeShowDelay_ShowsPicker()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Down(GestureKey.Down);

        Assert.True(result.Suppress);
        AssertSelection(SingleShow(result), 1);
        Assert.True(h.Machine.IsPickerVisible);
        Assert.Empty(h.Advance(Delay).Effects);
    }

    [Fact]
    public void UpThenRelease_PerformsDefaultActionOnSelectedItem()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Up);

        var release = h.Up(GestureKey.Modifier);

        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.Equal(new PerformAction(Id(2), 0), e));
    }

    [Fact]
    public void DownThenUpBackToDefault_ReleasePerformsActionBecauseSelectionChanged()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Up);

        var release = h.Up(GestureKey.Modifier);

        Assert.Equal(new PerformAction(Id(0), 0), release.Effects[^1]);
    }

    // ---- Left / Right and the actions pane ----

    [Fact]
    public void Right_OnItemWithActions_SwitchesToActionsPane()
    {
        var h = GestureHarness.WithItems(3, actionCount: 3);
        h.StartAndShow();

        var result = h.Down(GestureKey.Right);

        Assert.True(result.Suppress);
        AssertSelection(SingleUpdate(result), 0, actionIndex: 0, pane: PickerPane.Actions);
    }

    [Fact]
    public void Right_OnItemWithoutActions_StaysInItemsPane()
    {
        var items = new List<GestureItem> { new(Id(0), 0), new(Id(1), 2) };
        var h = new GestureHarness(items);
        h.StartAndShow();

        var result = h.Down(GestureKey.Right);

        Assert.True(result.Suppress);
        AssertSelection(SingleUpdate(result), 0, pane: PickerPane.Items);

        // Moving to an item that has actions makes Right work.
        h.Press(GestureKey.Down);
        AssertSelection(SingleUpdate(h.Down(GestureKey.Right)), 1, pane: PickerPane.Actions);
    }

    [Theory]
    [InlineData(3, 0, GestureKey.Down, 1)]
    [InlineData(3, 1, GestureKey.Down, 2)]
    [InlineData(3, 2, GestureKey.Down, 0)]
    [InlineData(3, 0, GestureKey.Up, 2)]
    [InlineData(3, 2, GestureKey.Up, 1)]
    [InlineData(1, 0, GestureKey.Down, 0)]
    [InlineData(1, 0, GestureKey.Up, 0)]
    public void UpDown_InActionsPane_MoveActionSelection_WithWrapAround_KeepingTheItem(int actionCount, int startAction, GestureKey key, int expectedAction)
    {
        var h = GestureHarness.WithItems(3, actionCount: actionCount);
        h.StartAndShow();
        h.Press(GestureKey.Down); // item 1
        h.Press(GestureKey.Right);
        for (var i = 0; i < startAction; i++)
        {
            h.Press(GestureKey.Down);
        }

        AssertSelection(h.Machine.CurrentSelection!, 1, startAction, PickerPane.Actions);

        var result = h.Down(key);

        AssertSelection(SingleUpdate(result), 1, expectedAction, PickerPane.Actions);
    }

    [Fact]
    public void ActionSelection_UsesTheSelectedItemsOwnActionCount()
    {
        var items = new List<GestureItem> { new(Id(0), 2), new(Id(1), 5) };
        var h = new GestureHarness(items);
        h.StartAndShow();
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Right);

        for (var i = 0; i < 4; i++)
        {
            h.Press(GestureKey.Down);
        }

        AssertSelection(h.Machine.CurrentSelection!, 1, 4, PickerPane.Actions);
        AssertSelection(SingleUpdate(h.Down(GestureKey.Down)), 1, 0, PickerPane.Actions);
    }

    [Fact]
    public void Left_SwitchesBackToItemsPane_KeepingTheChosenAction()
    {
        var h = GestureHarness.WithItems(3, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);

        var result = h.Down(GestureKey.Left);

        Assert.True(result.Suppress);
        AssertSelection(SingleUpdate(result), 0, actionIndex: 1, pane: PickerPane.Items);
    }

    [Fact]
    public void Left_InItemsPane_StaysInItemsPane()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        AssertSelection(SingleUpdate(h.Down(GestureKey.Left)), 0, pane: PickerPane.Items);
    }

    [Fact]
    public void ChangingItem_ResetsTheSelectedAction()
    {
        var h = GestureHarness.WithItems(3, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Left);
        AssertSelection(h.Machine.CurrentSelection!, 0, 2, PickerPane.Items);

        AssertSelection(SingleUpdate(h.Down(GestureKey.Down)), 1, 0, PickerPane.Items);
    }

    [Fact]
    public void MovingWithinSingleItemList_KeepsTheSelectedAction()
    {
        var h = GestureHarness.WithItems(1, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Left);

        // Down wraps onto the same item, so the item did not change and the action stays.
        AssertSelection(SingleUpdate(h.Down(GestureKey.Down)), 0, 1, PickerPane.Items);
    }

    [Fact]
    public void ChoosingNonDefaultActionOnDefaultItem_ReleasePerformsThatAction()
    {
        var h = GestureHarness.WithItems(3, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Down);

        var release = h.Up(GestureKey.Modifier);

        Assert.False(release.Suppress);
        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.Equal(new PerformAction(Id(0), 2), e));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void ReleaseInActionsPane_PerformsChosenActionOnChosenItem()
    {
        var h = GestureHarness.WithItems(4, actionCount: 5);
        h.StartAndShow();
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Up); // wraps to the last action

        var release = h.Up(GestureKey.Modifier);

        Assert.Equal(new PerformAction(Id(2), 4), release.Effects[^1]);
    }

    // ---- default (pinned) index ----

    [Theory]
    [InlineData(2, 3, 2)]
    [InlineData(5, 3, 2)]
    [InlineData(-1, 3, 0)]
    [InlineData(int.MinValue, 3, 0)]
    [InlineData(int.MaxValue, 3, 2)]
    [InlineData(0, 1, 0)]
    public void DefaultIndex_IsClampedIntoRange(int defaultIndex, int itemCount, int expected)
    {
        var h = GestureHarness.WithItems(itemCount, defaultIndex: defaultIndex);

        h.StartAndShow();

        AssertSelection(h.Machine.CurrentSelection!, expected);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-3)]
    public void DefaultIndex_WithEmptyHistory_IsHarmless(int defaultIndex)
    {
        var h = GestureHarness.WithItems(0, defaultIndex: defaultIndex);
        h.Start();
        h.Press(GestureKey.Down);
        h.Advance(TimeSpan.FromSeconds(1));

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void NonZeroDefaultIndex_QuickTap_PastesTheCurrentClipboard()
    {
        // e.g. pinned items come first and the most recent clipboard entry sits at index 2.
        var h = GestureHarness.WithItems(5, defaultIndex: 2);
        h.Start();

        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void NonZeroDefaultIndex_PickerStartsThere_AndReleaseUnchangedPastesTheCurrentClipboard()
    {
        var h = GestureHarness.WithItems(5, defaultIndex: 2);
        h.StartAndShow();
        AssertSelection(h.Machine.CurrentSelection!, 2);

        Assert.Collection(
            h.Up(GestureKey.Modifier).Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<PasteCurrentClipboard>(e));
    }

    [Fact]
    public void NonZeroDefaultIndex_SelectingAPinnedItemAbove_PerformsActionOnIt()
    {
        var h = GestureHarness.WithItems(5, defaultIndex: 2);
        h.StartAndShow();
        h.Press(GestureKey.Up);
        h.Press(GestureKey.Up);

        Assert.Equal(new PerformAction(Id(0), 0), h.Up(GestureKey.Modifier).Effects[^1]);
    }

    // ---- Delete ----

    [Fact]
    public void Delete_RemovesSelectedItem_AndSelectsTheNextOne()
    {
        var h = GestureHarness.WithItems(4);
        h.StartAndShow();
        h.Press(GestureKey.Down); // item 1

        var result = h.Down(GestureKey.Delete);

        Assert.True(result.Suppress);
        Assert.Collection(
            result.Effects,
            e => Assert.Equal(new DeleteItem(Id(1)), e),
            e =>
            {
                var selection = AssertUpdate(e);
                Assert.Equal([Id(0), Id(2), Id(3)], selection.ItemIds);
                AssertSelection(selection, 1);
                Assert.Equal(Id(2), selection.SelectedItemId);
            });
        Assert.True(h.Machine.IsActive);
    }

    [Fact]
    public void Delete_LastItemInList_ClampsSelectionToNewLastItem()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Up); // item 2 (last)

        var result = h.Down(GestureKey.Delete);

        Assert.Equal(new DeleteItem(Id(2)), result.Effects[0]);
        var selection = AssertUpdate(result.Effects[1]);
        Assert.Equal([Id(0), Id(1)], selection.ItemIds);
        AssertSelection(selection, 1);
    }

    [Fact]
    public void Delete_BeforePickerShown_ShowsPicker()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Down(GestureKey.Delete);

        Assert.Collection(
            result.Effects,
            e => Assert.Equal(new DeleteItem(Id(0)), e),
            e => AssertSelection(AssertShow(e), 0));
        Assert.True(h.Machine.IsPickerVisible);
    }

    [Fact]
    public void Delete_ResetsActionAndPane()
    {
        var h = GestureHarness.WithItems(3, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);

        var result = h.Down(GestureKey.Delete);

        Assert.Equal(new DeleteItem(Id(0)), result.Effects[0]);
        AssertSelection(AssertUpdate(result.Effects[1]), 0, 0, PickerPane.Items);
    }

    [Fact]
    public void Delete_OnlyItem_HidesPickerAndCancels()
    {
        var h = GestureHarness.WithItems(1);
        h.StartAndShow();

        var result = h.Down(GestureKey.Delete);

        Assert.True(result.Suppress);
        Assert.Collection(
            result.Effects,
            e => Assert.Equal(new DeleteItem(Id(0)), e),
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<GestureCancelled>(e));
        AssertIdle(h.Machine);

        // The modifier release that follows does nothing.
        var release = h.Up(GestureKey.Modifier);
        Assert.Equal(GestureResult.PassThrough, release);
    }

    [Fact]
    public void Delete_OnlyItem_BeforePickerShown_CancelsWithoutHide()
    {
        var h = GestureHarness.WithItems(1);
        h.Start();

        var result = h.Down(GestureKey.Delete);

        Assert.Collection(
            result.Effects,
            e => Assert.Equal(new DeleteItem(Id(0)), e),
            e => Assert.IsType<GestureCancelled>(e));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Delete_AllItemsOneByOne_EventuallyCancels()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        var deleted = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var result = h.Down(GestureKey.Delete);
            deleted.Add(Assert.IsType<DeleteItem>(result.Effects[0]).ItemId);
        }

        Assert.Equal([Id(0), Id(1), Id(2)], deleted);
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Delete_DefaultItem_ThenReleaseWithoutFurtherChanges_PerformsActionOnNewSelection()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Delete);

        var release = h.Up(GestureKey.Modifier);

        Assert.Collection(
            release.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.Equal(new PerformAction(Id(1), 0), e));
    }

    [Fact]
    public void Delete_ItemAboveTheDefault_KeepsTheDefaultPointingAtTheSameItem()
    {
        // Default is item 2. Move up to item 1 and delete it; the default must shift to index 1 (still item 2).
        var h = GestureHarness.WithItems(4, defaultIndex: 2);
        h.StartAndShow();
        h.Press(GestureKey.Up);
        var deleted = h.Down(GestureKey.Delete);
        Assert.Equal(new DeleteItem(Id(1)), deleted.Effects[0]);
        AssertSelection(AssertUpdate(deleted.Effects[1]), 1);
        Assert.Equal(Id(2), h.Machine.CurrentSelection!.SelectedItemId);

        // V advances from the current selection.
        AssertSelection(SingleUpdate(h.Press(GestureKey.Trigger)), 2);
    }

    [Fact]
    public void Delete_WithNonZeroDefault_DeletingLastItemsClampsEverything()
    {
        var h = GestureHarness.WithItems(3, defaultIndex: 2);
        h.StartAndShow();

        var first = h.Down(GestureKey.Delete);
        Assert.Equal(new DeleteItem(Id(2)), first.Effects[0]);
        AssertSelection(AssertUpdate(first.Effects[1]), 1);

        var second = h.Down(GestureKey.Delete);
        Assert.Equal(new DeleteItem(Id(1)), second.Effects[0]);
        var selection = AssertUpdate(second.Effects[1]);
        Assert.Equal([Id(0)], selection.ItemIds);
        AssertSelection(selection, 0);

        Assert.Equal(new PerformAction(Id(0), 0), h.Up(GestureKey.Modifier).Effects[^1]);
    }

    [Fact]
    public void Delete_DoesNotMutateTheListReturnedBySessionFactory()
    {
        var source = GestureHarness.CreateItems(3);
        var snapshot = source.ToArray();
        var machine = new PasteGestureStateMachine(() => (source, 0)) { ShowDelay = TimeSpan.Zero };
        machine.Handle(new KeyInput(GestureKey.Modifier, true), TimeSpan.Zero);
        machine.Handle(new KeyInput(GestureKey.Trigger, true), TimeSpan.Zero);

        machine.Handle(new KeyInput(GestureKey.Delete, true), TimeSpan.Zero);

        Assert.Equal(snapshot, source);
    }

    [Fact]
    public void ShowAndUpdate_CarryIndependentSnapshotsOfItemIds()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();
        var shown = SingleShow(h.Advance(Delay));

        h.Press(GestureKey.Delete);

        Assert.Equal([Id(0), Id(1), Id(2)], shown.ItemIds);
    }

    // ---- Escape and Cancel ----

    [Fact]
    public void Escape_WithPickerVisible_HidesAndCancels()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Down);

        var result = h.Down(GestureKey.Escape);

        Assert.True(result.Suppress);
        Assert.Collection(
            result.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<GestureCancelled>(e));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Escape_BeforePickerShown_CancelsWithoutHide()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Down(GestureKey.Escape);

        Assert.True(result.Suppress);
        Assert.IsType<GestureCancelled>(Assert.Single(result.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Escape_WithEmptyHistory_Cancels()
    {
        var h = GestureHarness.WithItems(0);
        h.Start();

        var result = h.Down(GestureKey.Escape);

        Assert.True(result.Suppress);
        Assert.IsType<GestureCancelled>(Assert.Single(result.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void AfterEscape_ModifierRelease_PastesNothing()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();
        h.Press(GestureKey.Escape);

        var release = h.Up(GestureKey.Modifier);

        Assert.Equal(GestureResult.PassThrough, release);
        Assert.Empty(h.Advance(TimeSpan.FromSeconds(1)).Effects);
    }

    [Fact]
    public void AfterEscape_ModifierStillHeld_NewTriggerPressStartsAFreshGesture()
    {
        var h = GestureHarness.WithItems(3, defaultIndex: 0);
        h.StartAndShow();
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Escape);

        Assert.True(h.Machine.IsModifierDown);
        Assert.True(h.Down(GestureKey.Trigger).Suppress);
        h.Up(GestureKey.Trigger);

        Assert.True(h.Machine.IsActive);
        Assert.Equal(2, h.SessionCount);
        AssertSelection(h.Machine.CurrentSelection!, 0);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void Cancel_WhenIdle_DoesNothing()
    {
        var h = GestureHarness.WithItems(3);

        Assert.Equal(GestureResult.PassThrough, h.Machine.Cancel());

        h.Down(GestureKey.Modifier);
        Assert.Equal(GestureResult.PassThrough, h.Machine.Cancel());
    }

    [Fact]
    public void Cancel_WithPickerVisible_HidesAndCancels_WithoutSuppression()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        var result = h.Machine.Cancel();

        Assert.False(result.Suppress);
        Assert.Collection(
            result.Effects,
            e => Assert.IsType<HidePicker>(e),
            e => Assert.IsType<GestureCancelled>(e));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Cancel_BeforePickerShown_OnlyCancels()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        var result = h.Machine.Cancel();

        Assert.False(result.Suppress);
        Assert.IsType<GestureCancelled>(Assert.Single(result.Effects));
        AssertIdle(h.Machine);
    }

    [Fact]
    public void Cancel_IsIdempotent()
    {
        var h = GestureHarness.WithItems(3);
        h.StartAndShow();

        h.Machine.Cancel();

        Assert.Equal(GestureResult.PassThrough, h.Machine.Cancel());
    }

    [Fact]
    public void Cancel_KeepsModifierState_AndPreventsTickFromShowing()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();

        h.Machine.Cancel();

        Assert.True(h.Machine.IsModifierDown);
        Assert.Empty(h.Advance(TimeSpan.FromSeconds(1)).Effects);
        Assert.Equal(GestureResult.PassThrough, h.Up(GestureKey.Modifier));
        Assert.Equal(GestureResult.PassThrough, h.Down(GestureKey.Down));
    }

    // ---- after the gesture ----

    [Fact]
    public void AfterRelease_StateIsFullyReset_ForTheNextGesture()
    {
        var h = GestureHarness.WithItems(4, actionCount: 3);
        h.StartAndShow();
        h.Press(GestureKey.Down);
        h.Press(GestureKey.Right);
        h.Press(GestureKey.Down);
        h.Up(GestureKey.Modifier);

        h.Start();

        var selection = Assert.IsType<PickerSelection>(h.Machine.CurrentSelection);
        AssertSelection(selection, 0, 0, PickerPane.Items);
        Assert.False(h.Machine.IsPickerVisible);
        Assert.IsType<PasteCurrentClipboard>(Assert.Single(h.Up(GestureKey.Modifier).Effects));
    }

    [Fact]
    public void NewGesture_UsesTheLatestHistory()
    {
        var h = GestureHarness.WithItems(2);
        h.Start();
        h.Up(GestureKey.Modifier);

        h.Items = GestureHarness.CreateItems(4);
        h.StartAndShow();

        Assert.Equal(4, h.Machine.CurrentSelection!.ItemIds.Count);
    }

    [Fact]
    public void ShowDelay_RestartsForEachGesture()
    {
        var h = GestureHarness.WithItems(3);
        h.Start();
        h.Advance(TimeSpan.FromMilliseconds(200));
        h.Up(GestureKey.Modifier);

        h.Advance(TimeSpan.FromMilliseconds(200));
        h.Start();

        Assert.Empty(h.Advance(TimeSpan.FromMilliseconds(200)).Effects);
        SingleShow(h.Advance(TimeSpan.FromMilliseconds(100)));
    }

    // ---- result helpers ----

    [Fact]
    public void GestureResult_StaticResults_HaveExpectedShape()
    {
        Assert.False(GestureResult.PassThrough.Suppress);
        Assert.Empty(GestureResult.PassThrough.Effects);
        Assert.True(GestureResult.Swallow.Suppress);
        Assert.Empty(GestureResult.Swallow.Effects);
        Assert.Same(GestureResult.PassThrough.Effects, GestureResult.PassThrough.Effects);
    }

    [Fact]
    public void PickerSelection_SelectedItemId_IndexesItemIds()
    {
        var selection = new PickerSelection([Id(0), Id(1), Id(2)], 2, 0, PickerPane.Items);

        Assert.Equal(Id(2), selection.SelectedItemId);
    }
}
