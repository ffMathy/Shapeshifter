using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Input;

namespace Shapeshifter.Core.Tests.Input;

public sealed class PickerSearchTests
{
    private static Guid Id(int index) => GestureHarness.IdFor(index);

    /// <summary>A search over items with these texts (item i has id <see cref="Id"/>(i) and <paramref name="actionCount"/> actions).</summary>
    private static PickerSearch Over(int actionCount, params string[] texts) =>
        new(texts.Select((text, i) => new SearchableItem(new GestureItem(Id(i), actionCount), HistorySearch.Normalize(text))).ToArray());

    private static PickerSearch Over(params string[] texts) => Over(3, texts);

    private static PickerSelection SingleUpdate(GestureResult result) =>
        Assert.IsType<UpdatePicker>(Assert.Single(result.Effects)).Selection;

    [Fact]
    public void Initially_EveryItemIsShown_WithTheFirstSelected()
    {
        var search = Over("apple", "banana", "cherry");

        var selection = search.Selection;

        Assert.Equal([Id(0), Id(1), Id(2)], selection.ItemIds);
        Assert.Equal(0, selection.SelectedIndex);
        Assert.Equal(string.Empty, selection.Query);
        Assert.True(selection.IsSearching);
    }

    [Fact]
    public void Typing_FiltersTheItems_InHistoryOrder_AndSelectsTheFirstMatch()
    {
        var search = Over("apple pie", "banana", "apple juice", "cherry");
        search.Handle(PickerSearchInput.Typed("an"));
        search.Handle(new PickerSearchInput(PickerSearchKey.Down));

        var result = search.Handle(PickerSearchInput.Typed("apple"));

        Assert.True(result.Suppress);
        var selection = SingleUpdate(result);
        Assert.Equal([Id(0), Id(2)], selection.ItemIds);
        Assert.Equal(0, selection.SelectedIndex);
        Assert.Equal("apple", selection.Query);
    }

    [Fact]
    public void Typing_TheSameQueryAgain_ChangesNothing()
    {
        var search = Over("apple", "banana");
        search.Handle(PickerSearchInput.Typed("a"));

        Assert.Equal(GestureResult.PassThrough, search.Handle(PickerSearchInput.Typed("a")));
    }

    [Fact]
    public void NoMatches_ShowsAnEmptyList_AndEnterDoesNothing()
    {
        var search = Over("apple", "banana");

        var selection = SingleUpdate(search.Handle(PickerSearchInput.Typed("kiwi")));
        var enter = search.Handle(new PickerSearchInput(PickerSearchKey.Enter));
        var down = search.Handle(new PickerSearchInput(PickerSearchKey.Down));
        var right = search.Handle(new PickerSearchInput(PickerSearchKey.Right));

        Assert.True(selection.IsEmpty);
        Assert.Equal("kiwi", selection.Query);
        Assert.Equal(GestureResult.Swallow, enter);
        Assert.Equal(GestureResult.Swallow, down);
        Assert.Equal(GestureResult.PassThrough, right);
        Assert.False(search.HasEnded);
    }

    [Theory]
    [InlineData(PickerSearchKey.Down, 1)]
    [InlineData(PickerSearchKey.Up, 2)]
    public void UpDown_MoveThroughTheResults_WrappingAround(PickerSearchKey key, int expected)
    {
        var search = Over("a", "b", "c");

        var selection = SingleUpdate(search.Handle(new PickerSearchInput(key)));

        Assert.Equal(expected, selection.SelectedIndex);
    }

    [Fact]
    public void Right_SwitchesToTheActions_UpDownMoveThem_LeftSwitchesBack()
    {
        var search = Over("a", "b");
        search.Handle(new PickerSearchInput(PickerSearchKey.Down));

        var right = SingleUpdate(search.Handle(new PickerSearchInput(PickerSearchKey.Right)));
        var up = SingleUpdate(search.Handle(new PickerSearchInput(PickerSearchKey.Up)));
        var left = SingleUpdate(search.Handle(new PickerSearchInput(PickerSearchKey.Left)));

        Assert.Equal(PickerPane.Actions, right.Pane);
        Assert.Equal(1, up.SelectedIndex);
        Assert.Equal(2, up.SelectedActionIndex);
        Assert.Equal(PickerPane.Items, left.Pane);
        Assert.Equal(2, left.SelectedActionIndex);
    }

    [Fact]
    public void LeftAndRight_AreLeftToTheSearchBox_WhenTheyHaveNothingToDo()
    {
        var search = Over(0, "a");

        // No actions to switch to, and already on the results.
        Assert.Equal(GestureResult.PassThrough, search.Handle(new PickerSearchInput(PickerSearchKey.Right)));
        Assert.Equal(GestureResult.PassThrough, search.Handle(new PickerSearchInput(PickerSearchKey.Left)));
    }

    [Fact]
    public void Right_WhileOnTheActions_IsLeftToTheSearchBox()
    {
        var search = Over("a");
        search.Handle(new PickerSearchInput(PickerSearchKey.Right));

        Assert.Equal(GestureResult.PassThrough, search.Handle(new PickerSearchInput(PickerSearchKey.Right)));
    }

    [Fact]
    public void Typing_WhileOnTheActions_GoesBackToTheResults()
    {
        var search = Over("apple", "avocado");
        search.Handle(new PickerSearchInput(PickerSearchKey.Right));
        search.Handle(new PickerSearchInput(PickerSearchKey.Down));

        var selection = SingleUpdate(search.Handle(PickerSearchInput.Typed("av")));

        Assert.Equal(PickerPane.Items, selection.Pane);
        Assert.Equal(0, selection.SelectedActionIndex);
        Assert.Equal([Id(1)], selection.ItemIds);
    }

    [Fact]
    public void Enter_GivesTheFocusBack_HidesThePicker_AndPerformsTheSelectedAction()
    {
        var search = Over("apple", "banana", "apricot");
        search.Handle(PickerSearchInput.Typed("ap"));
        search.Handle(new PickerSearchInput(PickerSearchKey.Down));
        search.Handle(new PickerSearchInput(PickerSearchKey.Right));
        search.Handle(new PickerSearchInput(PickerSearchKey.Down));

        var result = search.Handle(new PickerSearchInput(PickerSearchKey.Enter));

        Assert.True(result.Suppress);
        Assert.Collection(
            result.Effects,
            e => Assert.IsType<RestoreFocus>(e),
            e => Assert.IsType<HidePicker>(e),
            e => Assert.Equal(new PerformAction(Id(2), 1), e));
        Assert.True(search.HasEnded);
    }

    [Fact]
    public void Escape_GivesTheFocusBack_AndCancels()
    {
        var search = Over("apple");

        var result = search.Handle(new PickerSearchInput(PickerSearchKey.Escape));

        Assert.Collection(result.Effects, e => Assert.IsType<RestoreFocus>(e), e => Assert.IsType<HidePicker>(e), e => Assert.IsType<GestureCancelled>(e));
        Assert.True(search.HasEnded);
    }

    [Fact]
    public void LosingTheFocus_Cancels_WithoutTakingTheFocusBack()
    {
        var search = Over("apple");

        var result = search.Handle(new PickerSearchInput(PickerSearchKey.Deactivated));

        Assert.Collection(result.Effects, e => Assert.IsType<HidePicker>(e), e => Assert.IsType<GestureCancelled>(e));
    }

    [Fact]
    public void AfterTheSearchEnded_NothingHappens()
    {
        var search = Over("apple");
        search.Handle(new PickerSearchInput(PickerSearchKey.Escape));

        foreach (var key in Enum.GetValues<PickerSearchKey>())
        {
            Assert.Equal(GestureResult.PassThrough, search.Handle(new PickerSearchInput(key, "a")));
        }
    }

    [Fact]
    public void RandomInput_KeepsTheSelectionValid()
    {
        var random = new Random(458);
        var words = new[] { "", "a", "ap", "apple", "b", "zz", "an a" };
        for (var run = 0; run < 200; run++)
        {
            var count = random.Next(1, 6);
            var actions = random.Next(0, 4);
            var search = Over(actions, [.. Enumerable.Range(0, count).Select(i => i % 2 == 0 ? $"apple {i}" : $"banana {i}")]);
            for (var step = 0; step < 40 && !search.HasEnded; step++)
            {
                var key = (PickerSearchKey)random.Next(0, (int)PickerSearchKey.Enter);
                var result = search.Handle(new PickerSearchInput(key, words[random.Next(words.Length)]));
                foreach (var effect in result.Effects)
                {
                    Assert.IsType<UpdatePicker>(effect);
                }

                var selection = search.Selection;
                if (selection.IsEmpty)
                {
                    Assert.Equal(0, selection.SelectedIndex);
                    continue;
                }

                Assert.InRange(selection.SelectedIndex, 0, selection.ItemIds.Count - 1);
                if (selection.Pane == PickerPane.Actions)
                {
                    Assert.InRange(selection.SelectedActionIndex, 0, actions - 1);
                }
            }
        }
    }
}
