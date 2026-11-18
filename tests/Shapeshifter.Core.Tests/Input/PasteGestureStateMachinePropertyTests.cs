using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

/// <summary>Feeds long random key sequences (fixed seeds) and checks invariants after every step.</summary>
public sealed class PasteGestureStateMachinePropertyTests
{
    private static readonly GestureKey[] Keys =
    [
        GestureKey.Other, GestureKey.Modifier, GestureKey.Trigger, GestureKey.Up, GestureKey.Down,
        GestureKey.Left, GestureKey.Right, GestureKey.Delete, GestureKey.Escape, GestureKey.Search,
        GestureKey.Digit1, GestureKey.Digit3, GestureKey.Digit9, GestureKey.Space, GestureKey.Enter,
    ];

    [Theory]
    [InlineData(1, false)]
    [InlineData(7, false)]
    [InlineData(42, false)]
    [InlineData(1234, false)]
    [InlineData(98765, false)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(42, true)]
    [InlineData(1234, true)]
    [InlineData(98765, true)]
    public void RandomKeySequences_PreserveInvariants(int seed, bool pasteOnTriggerRelease)
    {
        var random = new Random(seed);
        for (var run = 0; run < 200; run++)
        {
            RunOne(random, pasteOnTriggerRelease);
        }
    }

    private static void RunOne(Random random, bool pasteOnTriggerRelease)
    {
        var itemCount = random.Next(0, 6);
        var items = Enumerable.Range(0, itemCount)
            .Select(i => new GestureItem(GestureHarness.IdFor(i), random.Next(0, 4)))
            .ToList();
        var defaultIndex = random.Next(-2, itemCount + 2);
        var showDelay = random.Next(4) == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(random.Next(1, 500));
        var harness = new GestureHarness(items, defaultIndex, showDelay, pasteOnTriggerRelease);
        var machine = harness.Machine;

        var known = items.ToDictionary(i => i.Id);
        var deleted = new HashSet<Guid>();
        var pickerShown = false;
        var physicallyDown = new HashSet<GestureKey>();

        for (var step = 0; step < 60; step++)
        {
            var wasPickerVisible = machine.IsPickerVisible;
            var wasActive = machine.IsActive;
            var wasHeldOpen = machine.IsHeldOpen;
            var wasModifierDown = machine.IsModifierDown;
            GestureResult result;
            var roll = random.Next(10);
            GestureKey? key = null;
            var isDown = false;
            if (roll == 0)
            {
                result = harness.Advance(TimeSpan.FromMilliseconds(random.Next(0, 400)));
            }
            else if (roll == 1 && random.Next(4) == 0)
            {
                result = machine.Cancel();
                Assert.False(result.Suppress);
            }
            else
            {
                key = Keys[random.Next(Keys.Length)];

                // Mostly realistic: toggle the key's physical state, sometimes an auto-repeat key-down.
                isDown = !physicallyDown.Contains(key.Value) || random.Next(3) == 0;
                if (isDown)
                {
                    physicallyDown.Add(key.Value);
                }
                else
                {
                    physicallyDown.Remove(key.Value);
                }

                result = isDown ? harness.Down(key.Value) : harness.Up(key.Value);
            }

            // Effect-level invariants.
            var terminal = 0;
            foreach (var effect in result.Effects)
            {
                switch (effect)
                {
                    case ShowPicker show:
                        Assert.False(pickerShown, "Show emitted while the picker was already shown.");
                        pickerShown = true;
                        AssertSelectionValid(show.Selection, known, deleted);
                        break;
                    case UpdatePicker update:
                        Assert.True(pickerShown, "Update emitted while the picker was hidden.");
                        AssertSelectionValid(update.Selection, known, deleted);
                        break;
                    case HidePicker:
                        Assert.True(pickerShown, "Hide emitted while the picker was hidden.");
                        pickerShown = false;
                        break;
                    case DeleteItem delete:
                        Assert.True(known.ContainsKey(delete.ItemId));
                        Assert.True(deleted.Add(delete.ItemId), "The same item was deleted twice.");
                        break;
                    case PerformAction perform:
                        terminal++;
                        Assert.True(known.ContainsKey(perform.ItemId));
                        Assert.DoesNotContain(perform.ItemId, deleted);
                        var actionCount = known[perform.ItemId].ActionCount;
                        Assert.True(perform.ActionIndex == 0 || perform.ActionIndex < actionCount);
                        Assert.True(perform.ActionIndex >= 0);
                        break;
                    case PasteCurrentClipboard:
                    case GestureCancelled:
                        terminal++;
                        break;
                    case BeginSearch begin:
                        // The picker stays open, but it now belongs to the search rather than to the gesture.
                        terminal++;
                        Assert.True(pickerShown, "A search began without the picker.");
                        Assert.NotEmpty(begin.Selection.ItemIds);
                        AssertSelectionValid(begin.Selection, known, deleted);
                        pickerShown = false;
                        break;
                    default:
                        Assert.Fail($"Unexpected effect {effect}.");
                        break;
                }
            }

            Assert.True(terminal <= 1, "More than one terminal effect in one result.");
            if (terminal == 1)
            {
                Assert.False(pickerShown, "The gesture ended while the picker was still shown.");
                Assert.False(machine.IsActive);
                Assert.IsNotType<HidePicker>(result.Effects[^1]);
            }

            // State-level invariants.
            Assert.Equal(pickerShown, machine.IsPickerVisible);
            Assert.True(!machine.IsPickerVisible || machine.IsActive);
            Assert.True(!machine.IsHeldOpen || (machine.IsActive && machine.IsPickerVisible), "Held open without a visible picker.");
            if (machine.CurrentSelection is { } current)
            {
                Assert.True(machine.IsActive);
                AssertSelectionValid(current, known, deleted);
            }

            if (wasActive && !machine.IsActive)
            {
                Assert.Equal(1, terminal);
                deleted.Clear(); // a new gesture re-reads the full history
            }

            if (key == GestureKey.Modifier)
            {
                Assert.Equal(isDown, machine.IsModifierDown);
                Assert.False(result.Suppress, "Modifier events must never be suppressed.");
                if (!isDown && wasHeldOpen)
                {
                    // A picker kept open (Space) survives the release and waits for Enter or Escape.
                    Assert.Equal(GestureResult.PassThrough, result);
                    Assert.True(machine.IsPickerVisible);
                }
                else if (!isDown)
                {
                    Assert.False(machine.IsActive, "The machine must be idle after the modifier is released.");
                    Assert.False(machine.IsPickerVisible);
                    Assert.Null(machine.CurrentSelection);
                }
            }

            if (key == GestureKey.Trigger && !isDown && wasActive)
            {
                // Releasing V pastes the current clipboard, unless the picker is up (issue #639).
                if (pasteOnTriggerRelease && !wasPickerVisible)
                {
                    Assert.IsType<PasteCurrentClipboard>(Assert.Single(result.Effects));
                    Assert.False(machine.IsActive);
                }
                else
                {
                    Assert.Empty(result.Effects);
                    Assert.True(machine.IsActive);
                }

                Assert.True(result.Suppress);
            }

            if (key is GestureKey.Other)
            {
                Assert.Equal(GestureResult.PassThrough, result);
            }

            if (key is { } k && k != GestureKey.Modifier && k != GestureKey.Other && isDown)
            {
                // While a gesture is running every gesture key-down is suppressed (except V without the modifier, while
                // the picker is kept open: that is typing); when idle only V can be.
                if (wasActive && k == GestureKey.Trigger && wasHeldOpen && !wasModifierDown)
                {
                    Assert.Equal(GestureResult.PassThrough, result);
                }
                else if (wasActive)
                {
                    Assert.True(result.Suppress);
                }
                else if (k != GestureKey.Trigger)
                {
                    Assert.Equal(GestureResult.PassThrough, result);
                }
            }

            if (key is null && result.Effects.Count > 0)
            {
                // A tick or Cancel never asks to suppress anything.
                Assert.False(result.Suppress);
            }
        }

        // Releasing the modifier always ends with an idle machine, unless the picker is kept open: then Escape does.
        harness.Up(GestureKey.Modifier);
        if (machine.IsHeldOpen)
        {
            Assert.True(machine.IsPickerVisible);
            harness.Down(GestureKey.Escape);
        }

        Assert.False(machine.IsActive);
        Assert.False(machine.IsPickerVisible);
    }

    private static void AssertSelectionValid(PickerSelection selection, Dictionary<Guid, GestureItem> known, HashSet<Guid> deleted)
    {
        if (selection.IsEmpty)
        {
            // Only an empty history shows an empty picker; deleting the last item ends the gesture instead.
            Assert.Empty(known);
            Assert.Equal(0, selection.SelectedIndex);
            Assert.Equal(PickerPane.Items, selection.Pane);
            return;
        }

        Assert.InRange(selection.SelectedIndex, 0, selection.ItemIds.Count - 1);
        Assert.Equal(selection.ItemIds.Count, selection.ItemIds.Distinct().Count());
        foreach (var id in selection.ItemIds)
        {
            Assert.True(known.ContainsKey(id));
            Assert.DoesNotContain(id, deleted);
        }

        var item = known[selection.SelectedItemId];
        Assert.True(selection.SelectedActionIndex >= 0);
        if (selection.Pane == PickerPane.Actions)
        {
            Assert.True(item.ActionCount > 0, "Actions pane shown for an item without actions.");
            Assert.InRange(selection.SelectedActionIndex, 0, item.ActionCount - 1);
        }
        else
        {
            Assert.True(selection.SelectedActionIndex == 0 || selection.SelectedActionIndex < item.ActionCount);
        }
    }
}
