using System.Reflection;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog.Core;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Tests;

/// <summary>
/// The picker's lists must always show the selection the engine asked for. A list that got an index while it was empty
/// (the first picker, an empty history, a search without matches) used to keep showing no selection afterwards, so
/// holding Ctrl+V sometimes showed the picker with no item selected.
/// </summary>
public sealed class PickerPresenterSelectionTests
{
    [Fact]
    public async Task First_picker_selects_the_newest_item()
    {
        await RunAsync(presenter =>
        {
            Show(presenter, Items(3), selectedIndex: 0, actionCount: 2);

            AssertSelection(presenter, 0, 0);
        });
    }

    [Fact]
    public async Task Picker_selects_the_newest_item_after_the_history_was_empty()
    {
        await RunAsync(presenter =>
        {
            Show(presenter, [], selectedIndex: 0, actionCount: 0);
            Hide(presenter);

            Show(presenter, Items(2), selectedIndex: 0, actionCount: 2);

            AssertSelection(presenter, 0, 0);
        });
    }

    [Fact]
    public async Task Picker_selects_the_newest_unpinned_item_below_a_pinned_one()
    {
        await RunAsync(presenter =>
        {
            Show(presenter, Items(1), selectedIndex: 0, actionCount: 1);
            Hide(presenter);

            Show(presenter, [Item("pinned", pinned: true), .. Items(2)], selectedIndex: 1, actionCount: 2);

            AssertSelection(presenter, 1, 0);
        });
    }

    [Fact]
    public async Task Search_selects_the_first_match_after_finding_nothing()
    {
        await RunAsync(presenter =>
        {
            var items = Items(3);
            Show(presenter, items, selectedIndex: 0, actionCount: 2);
            Update(presenter, [], selectedIndex: 0, actionCount: 0);

            Update(presenter, items[1..], selectedIndex: 0, actionCount: 2);

            AssertSelection(presenter, 0, 0);
        });
    }

    [Fact]
    public async Task Lists_always_show_the_requested_selection()
    {
        await RunAsync(presenter =>
        {
            var random = new Random(1);
            var pool = Enumerable.Range(0, 6).Select(i => Item("item " + i, pinned: i < 2)).ToArray();
            var visible = false;
            for (var step = 0; step < 300; step++)
            {
                var count = random.Next(5) == 0 ? 0 : random.Next(1, pool.Length + 1);
                var items = pool.OrderBy(_ => random.Next()).Take(count).ToArray();
                var selectedIndex = count == 0 ? 0 : random.Next(count);
                var actionCount = random.Next(4);
                var actionIndex = actionCount == 0 ? 0 : random.Next(actionCount);
                switch (visible ? random.Next(3) : 0)
                {
                    case 0:
                        Show(presenter, items, selectedIndex, actionCount, actionIndex);
                        visible = true;
                        break;
                    case 1:
                        Update(presenter, items, selectedIndex, actionCount, actionIndex);
                        break;
                    default:
                        Hide(presenter);
                        visible = false;
                        continue;
                }

                AssertSelection(presenter, count == 0 ? -1 : selectedIndex, actionCount == 0 ? -1 : actionIndex);
            }
        });
    }

    private static Task RunAsync(Action<PickerPresenter> test) => TestApp.RunAsync(() =>
    {
        var presenter = new PickerPresenter(new NoCursor(), new PickerVisuals(new NoFileIcons(), Logger.None), "Ctrl", Logger.None);
        try
        {
            test(presenter);
        }
        finally
        {
            Hide(presenter);
        }
    });

    private static void Show(PickerPresenter presenter, ClipboardItem[] items, int selectedIndex, int actionCount, int actionIndex = 0) =>
        Run(presenter.ShowAsync(State(items, selectedIndex, actionCount, actionIndex)));

    private static void Update(PickerPresenter presenter, ClipboardItem[] items, int selectedIndex, int actionCount, int actionIndex = 0) =>
        Run(presenter.UpdateAsync(State(items, selectedIndex, actionCount, actionIndex)));

    private static void Hide(PickerPresenter presenter) => Run(presenter.HideAsync());

    private static void Run(Task task)
    {
        Dispatcher.UIThread.RunJobs();
        Assert.True(task.IsCompletedSuccessfully);
    }

    private static PickerViewState State(ClipboardItem[] items, int selectedIndex, int actionCount, int actionIndex) => new(
        items,
        selectedIndex,
        Enumerable.Range(0, actionCount).Select(i => new PickerActionView("action" + i, "Action " + i, string.Empty)).ToArray(),
        actionIndex,
        PickerPane.Items);

    private static void AssertSelection(PickerPresenter presenter, int itemIndex, int actionIndex)
    {
        var window = (Window)typeof(PickerPresenter).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(presenter)!;
        var lists = window.GetVisualDescendants().OfType<ListBox>().ToArray();
        Assert.Equal(itemIndex, lists.Single(list => list.Name == "ItemList").SelectedIndex);
        Assert.Equal(actionIndex, lists.Single(list => list.Classes.Contains("actions")).SelectedIndex);
    }

    private static ClipboardItem[] Items(int count) =>
        Enumerable.Range(0, count).Select(i => Item("text " + i)).ToArray();

    private static ClipboardItem Item(string text, bool pinned = false)
    {
        var item = new ClipboardItem(new ClipboardSnapshot { Text = text });

        // Pinning is the history's business; only how the row looks matters here.
        typeof(ClipboardItem).GetProperty(nameof(ClipboardItem.IsPinned))!.SetValue(item, pinned);
        return item;
    }

    private sealed class NoFileIcons : IFileIconProvider
    {
        public byte[]? GetIconPng(string path, int size) => null;
    }

    private sealed class NoCursor : ICursorLocator
    {
        public (int X, int Y)? GetCursorPosition() => null;
    }
}
