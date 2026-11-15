using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Tests.Input;

/// <summary>Feeds long random key sequences (fixed seeds) and checks invariants after every step.</summary>
public sealed class PasteGestureStateMachinePropertyTests
{
    private static readonly GestureKey[] Keys =
    [
        GestureKey.Other, GestureKey.Modifier, GestureKey.Trigger, GestureKey.Up, GestureKey.Down,
        GestureKey.Left, GestureKey.Right, GestureKey.Delete, GestureKey.Escape,
    ];

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1234)]
    [InlineData(98765)]
    public void RandomKeySequences_PreserveInvariants(int seed)
    {
        var random = new Random(seed);
        for (var run = 0; run < 200; run++)
        {
            RunOne(random);
        }
    }

    private static void RunOne(Random random)
    {
        var itemCount = random.Next(0, 6);
        var items = Enumerable.Range(0, itemCount)
            .Select(i => new GestureItem(GestureHarness.IdFor(i), random.Next(0, 4)))
            .ToList();
        var defaultIndex = random.Next(-2, itemCount + 2);
        var showDelay = random.Next(4) == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(random.Next(1, 500));
        var harness = new GestureHarness(items, defaultIndex, showDelay);
        var machine = harness.Machine;

        var known = items.ToDictionary(i => i.Id);
        var deleted = new HashSet<Guid>();
        var pickerShown = false;
        var physicallyDown = new HashSet<GestureKey>();

        for (var step = 0; step < 60; step++)
        {
            var wasActive = machine.IsActive;
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
                if (!isDown)
                {
                    Assert.False(machine.IsActive, "The machine must be idle after the modifier is released.");
                    Assert.False(machine.IsPickerVisible);
                    Assert.Null(machine.CurrentSelection);
                }
            }

            if (key is GestureKey.Other)
            {
                Assert.Equal(GestureResult.PassThrough, result);
            }

            if (key is { } k && k != GestureKey.Modifier && k != GestureKey.Other && isDown)
            {
                // While a gesture is running every gesture key-down is suppressed; when idle only V can be.
                if (wasActive)
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

        // Releasing the modifier always ends with an idle machine.
        harness.Up(GestureKey.Modifier);
        Assert.False(machine.IsActive);
        Assert.False(machine.IsPickerVisible);
    }

    private static void AssertSelectionValid(PickerSelection selection, Dictionary<Guid, GestureItem> known, HashSet<Guid> deleted)
    {
        Assert.NotEmpty(selection.ItemIds);
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
