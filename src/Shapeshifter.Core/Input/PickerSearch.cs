using Shapeshifter.Core.Clipboard;

namespace Shapeshifter.Core.Input;

/// <summary>What the user did in the picker while searching.</summary>
public enum PickerSearchKey
{
    /// <summary>The text in the search box changed (<see cref="PickerSearchInput.Query"/>).</summary>
    QueryChanged,
    Up,
    Down,

    /// <summary>Back from the actions to the results.</summary>
    Left,

    /// <summary>To the actions of the selected result.</summary>
    Right,

    /// <summary>Perform the selected action on the selected result.</summary>
    Enter,

    /// <summary>Close the picker without doing anything.</summary>
    Escape,

    /// <summary>The picker lost the keyboard focus, for example because the user clicked another window.</summary>
    Deactivated,
}

public readonly record struct PickerSearchInput(PickerSearchKey Key, string? Query = null)
{
    public static PickerSearchInput Typed(string query) => new(PickerSearchKey.QueryChanged, query);
}

/// <summary>An item that can be searched: its id, how many actions it offers and its normalized search text.</summary>
public readonly record struct SearchableItem(GestureItem Item, string SearchText);

/// <summary>
/// Searching the history in the picker (issue #458), as a pure state machine driven by what the user does in the
/// focused picker.
/// </summary>
/// <remarks>
/// <para>
/// The search starts when S is pressed during the gesture of <see cref="PasteGestureStateMachine"/>. The
/// picker then has the keyboard focus, so the user types freely, in any keyboard layout. Every item that matches
/// what was typed (<see cref="HistorySearch"/>) is shown, in the order of the history, with the first one selected.
/// </para>
/// <list type="bullet">
/// <item>Up/Down move through the results (wrapping around), or through the actions while they are active.</item>
/// <item>Right switches to the actions of the selected result, Left switches back.</item>
/// <item>Enter gives the focus back to the window the user came from and performs the action.</item>
/// <item>Escape gives the focus back and does nothing; losing the focus to another window does nothing either.</item>
/// </list>
/// <para>The machine is not thread-safe; the caller serializes calls.</para>
/// </remarks>
public sealed class PickerSearch
{
    private readonly IReadOnlyList<SearchableItem> items;
    private List<GestureItem> results;
    private int selectedIndex;
    private int selectedActionIndex;
    private PickerPane pane = PickerPane.Items;
    private bool ended;

    public PickerSearch(IReadOnlyList<SearchableItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.items = items;
        results = items.Select(item => item.Item).ToList();
    }

    public string Query { get; private set; } = string.Empty;

    /// <summary>Whether the search has ended (an action was chosen, or it was cancelled).</summary>
    public bool HasEnded => ended;

    public PickerSelection Selection =>
        new(results.Select(item => item.Id).ToArray(), selectedIndex, selectedActionIndex, pane) { Query = Query };

    public GestureResult Handle(PickerSearchInput input)
    {
        if (ended)
        {
            return GestureResult.PassThrough;
        }

        switch (input.Key)
        {
            case PickerSearchKey.QueryChanged:
                return SetQuery(input.Query ?? string.Empty);
            case PickerSearchKey.Up:
                return Move(-1);
            case PickerSearchKey.Down:
                return Move(+1);
            case PickerSearchKey.Right:
                if (results.Count == 0 || pane == PickerPane.Actions || results[selectedIndex].ActionCount == 0)
                {
                    return GestureResult.PassThrough;
                }

                pane = PickerPane.Actions;
                return Updated();
            case PickerSearchKey.Left:
                if (pane == PickerPane.Items)
                {
                    return GestureResult.PassThrough;
                }

                pane = PickerPane.Items;
                return Updated();
            case PickerSearchKey.Enter:
                if (results.Count == 0)
                {
                    return GestureResult.Swallow;
                }

                return End(new RestoreFocus(), new HidePicker(), new PerformAction(results[selectedIndex].Id, selectedActionIndex));
            case PickerSearchKey.Escape:
                return End(new RestoreFocus(), new HidePicker(), new GestureCancelled());
            case PickerSearchKey.Deactivated:
                // The user went to another window on purpose; leave the focus there.
                return End(new HidePicker(), new GestureCancelled());
            default:
                return GestureResult.PassThrough;
        }
    }

    private GestureResult SetQuery(string query)
    {
        if (query == Query)
        {
            return GestureResult.PassThrough;
        }

        Query = query;
        var terms = HistorySearch.ParseQuery(query);
        results = items.Where(item => HistorySearch.Matches(item.SearchText, terms)).Select(item => item.Item).ToList();
        selectedIndex = 0;
        selectedActionIndex = 0;
        pane = PickerPane.Items;
        return Updated();
    }

    private GestureResult Move(int delta)
    {
        if (results.Count == 0)
        {
            return GestureResult.Swallow;
        }

        if (pane == PickerPane.Actions)
        {
            var count = results[selectedIndex].ActionCount;
            selectedActionIndex = PasteGestureStateMachine.Wrap(selectedActionIndex + delta, count);
        }
        else
        {
            selectedIndex = PasteGestureStateMachine.Wrap(selectedIndex + delta, results.Count);
            selectedActionIndex = 0;
        }

        return Updated();
    }

    private GestureResult Updated() => new(true, [new UpdatePicker(Selection)]);

    private GestureResult End(params GestureEffect[] effects)
    {
        ended = true;
        return new GestureResult(true, effects);
    }
}
