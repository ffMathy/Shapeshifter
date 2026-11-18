using Shapeshifter.Core.Platform;

namespace Shapeshifter.Core.Input;

public enum PickerPane
{
    Items,
    Actions,
}

/// <summary>An entry the gesture can select: an item id and how many actions it offers.</summary>
public readonly record struct GestureItem(Guid Id, int ActionCount);

/// <summary>What the picker should currently display.</summary>
/// <param name="IsHeldOpen">
/// The user pressed Space, so the picker stays open after the modifier is released: Enter pastes and Escape closes it.
/// </param>
/// <remarks>
/// <para>With an empty history the picker is shown without items (<see cref="IsEmpty"/>), to explain how to use it.</para>
/// <para>
/// While the user searches (issue #458), <see cref="Query"/> holds what they typed and <see cref="ItemIds"/> only the
/// items that match it, which may be none.
/// </para>
/// </remarks>
public sealed record PickerSelection(
    IReadOnlyList<Guid> ItemIds,
    int SelectedIndex,
    int SelectedActionIndex,
    PickerPane Pane,
    bool IsHeldOpen = false)
{
    public bool IsEmpty => ItemIds.Count == 0;

    /// <summary>What the user typed in the search box, or <see langword="null"/> while not searching.</summary>
    public string? Query { get; init; }

    public bool IsSearching => Query is not null;

    /// <summary>The selected item. Not available when <see cref="IsEmpty"/>.</summary>
    public Guid SelectedItemId => ItemIds[SelectedIndex];
}

public abstract record GestureEffect;

/// <summary>Show the picker (it was hidden).</summary>
public sealed record ShowPicker(PickerSelection Selection) : GestureEffect;

/// <summary>The picker is visible and its selection or pane changed.</summary>
public sealed record UpdatePicker(PickerSelection Selection) : GestureEffect;

public sealed record HidePicker : GestureEffect;

/// <summary>Perform action <paramref name="ActionIndex"/> of item <paramref name="ItemId"/>.</summary>
public sealed record PerformAction(Guid ItemId, int ActionIndex) : GestureEffect;

/// <summary>
/// Paste whatever is on the clipboard right now, exactly as the user's own keystroke would have.
/// Used when the user did not pick anything else, so the current clipboard is pasted untouched.
/// </summary>
public sealed record PasteCurrentClipboard : GestureEffect;

/// <summary>The user deleted an item from the history while the picker was open.</summary>
public sealed record DeleteItem(Guid ItemId) : GestureEffect;

/// <summary>The gesture was cancelled; nothing is pasted.</summary>
public sealed record GestureCancelled : GestureEffect;

/// <summary>
/// S was pressed: the picker stays open, takes the keyboard focus and lets the user type to search the history (see
/// <see cref="PickerSearch"/>). The gesture itself has ended.
/// </summary>
public sealed record BeginSearch(PickerSelection Selection) : GestureEffect;

/// <summary>Give the keyboard focus back to the window that had it before the search began.</summary>
public sealed record RestoreFocus : GestureEffect;

public readonly record struct GestureResult(bool Suppress, IReadOnlyList<GestureEffect> Effects)
{
    public static GestureResult PassThrough { get; } = new(false, []);

    public static GestureResult Swallow { get; } = new(true, []);
}

/// <summary>
/// The paste gesture, as a pure state machine that is driven by key events and clock ticks.
/// </summary>
/// <remarks>
/// <para>Hold the modifier (Ctrl, or Cmd on macOS) and press V:</para>
/// <list type="bullet">
/// <item>
/// Releasing V (or the modifier) right away pastes the current clipboard, just like a normal paste. Releasing V is
/// enough while the modifier is still held (<see cref="PasteOnTriggerRelease"/>), so the user can paste repeatedly
/// with the modifier held down, as without Shapeshifter (issue #639).
/// </item>
/// <item>
/// Holding V (with the modifier) for <see cref="ShowDelay"/> shows the picker with the history. With nothing in the
/// history yet, the picker is shown empty, so it can explain how to use it; releasing still pastes normally.
/// </item>
/// <item>Each further press of V (with the modifier still held) selects the next, older item and shows the picker.</item>
/// <item>Up/Down move the selection, Right switches to the actions of the selected item, Left switches back.</item>
/// <item>1 to 9 jump to the item with that number (the first nine items, top first).</item>
/// <item>Delete removes the selected item; Escape cancels.</item>
/// <item>S starts searching the history (<see cref="BeginSearch"/>): the gesture ends and the picker stays open.</item>
/// <item>Releasing the modifier performs the selected action (normally "Paste") on the selected item.</item>
/// <item>
/// Space keeps the picker open after the modifier is released, to look through the history at leisure (issue #420).
/// The navigation keys then work without the modifier, Enter performs the selected action and Escape closes the picker.
/// </item>
/// <item>Enter performs the selected action right away, whether or not the picker is kept open.</item>
/// <item>V pressed together with Shift, Alt/AltGr or another modifier is left alone (Ctrl+Shift+V, AltGr+V).</item>
/// </list>
/// <para>
/// The machine is not thread-safe; the caller serializes calls. Times are monotonic offsets supplied by the caller.
/// </para>
/// </remarks>
public sealed class PasteGestureStateMachine
{
    private readonly Func<(IReadOnlyList<GestureItem> Items, int DefaultIndex)> sessionFactory;

    private bool modifierDown;
    private bool shiftDown;
    private bool altDown;
    private bool otherModifierDown;
    private bool triggerDown;
    private bool active;
    private bool pickerVisible;
    private bool heldOpen;
    private bool selectionChanged;
    private TimeSpan startedAt;
    private List<GestureItem> items = [];
    private int defaultIndex;
    private int selectedIndex;
    private int selectedActionIndex;
    private PickerPane pane;

    public PasteGestureStateMachine(Func<(IReadOnlyList<GestureItem> Items, int DefaultIndex)> sessionFactory)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        this.sessionFactory = sessionFactory;
    }

    /// <summary>
    /// How long after V is pressed the picker appears, while V is still held (or, without
    /// <see cref="PasteOnTriggerRelease"/>, the modifier). Zero shows it immediately.
    /// </summary>
    public TimeSpan ShowDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Whether releasing V before the picker is shown pastes right away, while the modifier is still held (issue #639).
    /// This needs the gesture's modifier to be the one that pastes: with the alternative shortcuts (Win+V on Windows,
    /// Ctrl+V on macOS) the held modifier would turn the paste keystroke into another shortcut, so the paste waits for
    /// the modifier to be released, and the picker also appears when only the modifier is kept held.
    /// </summary>
    public bool PasteOnTriggerRelease { get; set; } = true;

    /// <summary>Whether a gesture is in progress (the paste keystroke has been intercepted).</summary>
    public bool IsActive => active;

    public bool IsPickerVisible => pickerVisible;

    public bool IsModifierDown => modifierDown;

    /// <summary>Whether the picker stays open after the modifier is released (the user pressed Space).</summary>
    public bool IsHeldOpen => heldOpen;

    public PickerSelection? CurrentSelection => active && items.Count > 0 ? CreateSelection() : null;

    public GestureResult Handle(KeyInput input, TimeSpan now)
    {
        switch (input.Key)
        {
            case GestureKey.Modifier:
                return input.IsDown ? OnModifierDown() : OnModifierUp();
            case GestureKey.Trigger:
                return input.IsDown ? OnTriggerDown(now) : OnTriggerUp();
            case GestureKey.Shift:
                shiftDown = input.IsDown;
                return GestureResult.PassThrough;
            case GestureKey.Alt:
                altDown = input.IsDown;
                return GestureResult.PassThrough;
            case GestureKey.OtherModifier:
                otherModifierDown = input.IsDown;
                return GestureResult.PassThrough;
            case GestureKey.Up:
            case GestureKey.Down:
            case GestureKey.Left:
            case GestureKey.Right:
            case GestureKey.Delete:
            case GestureKey.Escape:
            case GestureKey.Search:
            case GestureKey.Space:
            case GestureKey.Enter:
            case >= GestureKey.Digit1 and <= GestureKey.Digit9:
                return OnNavigationKey(input);
            default:
                return GestureResult.PassThrough;
        }
    }

    /// <summary>Advances time; shows the picker once the modifier has been held long enough.</summary>
    public GestureResult Tick(TimeSpan now)
    {
        if (!active || pickerVisible || now - startedAt < ShowDelay)
        {
            return GestureResult.PassThrough;
        }

        return new GestureResult(false, [ShowOrUpdate()]);
    }

    /// <summary>Cancels a gesture from the outside (for example when the hook was interrupted).</summary>
    public GestureResult Cancel()
    {
        if (!active)
        {
            return GestureResult.PassThrough;
        }

        var effects = new List<GestureEffect>();
        if (pickerVisible)
        {
            effects.Add(new HidePicker());
        }

        effects.Add(new GestureCancelled());
        Reset();
        return new GestureResult(false, effects);
    }

    private GestureResult OnModifierDown()
    {
        modifierDown = true;
        return GestureResult.PassThrough;
    }

    private GestureResult OnModifierUp()
    {
        modifierDown = false;
        if (!active || heldOpen)
        {
            // A picker that is kept open waits for Enter or Escape.
            return GestureResult.PassThrough;
        }

        // The modifier release itself always reaches the focused application.
        return new GestureResult(false, Finish());
    }

    /// <summary>Hides the picker and pastes: the selected action, or the current clipboard if nothing was chosen.</summary>
    private List<GestureEffect> Finish()
    {
        var effects = new List<GestureEffect>(2);
        if (pickerVisible)
        {
            effects.Add(new HidePicker());
        }

        if (items.Count == 0 || (!selectionChanged && selectedIndex == defaultIndex && selectedActionIndex == 0))
        {
            effects.Add(new PasteCurrentClipboard());
        }
        else
        {
            effects.Add(new PerformAction(items[selectedIndex].Id, selectedActionIndex));
        }

        Reset();
        return effects;
    }

    private GestureResult OnTriggerDown(TimeSpan now)
    {
        if (triggerDown && !active)
        {
            // Auto-repeat of a V press that started a gesture which has since ended (pasted or cancelled):
            // its key-down was suppressed, so its repeats must not reach the application or restart a gesture.
            return GestureResult.Swallow;
        }

        if (!modifierDown)
        {
            return GestureResult.PassThrough;
        }

        if (!active && (shiftDown || altDown || otherModifierDown))
        {
            // Ctrl+Shift+V (paste in terminals, paste without formatting), AltGr+V (characters such as '@' on some
            // layouts) and similar shortcuts belong to the focused application.
            return GestureResult.PassThrough;
        }

        if (!active)
        {
            Begin(now);
            triggerDown = true;
            if (ShowDelay <= TimeSpan.Zero)
            {
                return new GestureResult(true, [ShowOrUpdate()]);
            }

            return GestureResult.Swallow;
        }

        if (triggerDown)
        {
            // Auto-repeat while V is held.
            return GestureResult.Swallow;
        }

        triggerDown = true;
        if (items.Count == 0)
        {
            // Nothing to select: just make sure the (empty) picker explains itself.
            return pickerVisible ? GestureResult.Swallow : new GestureResult(true, [ShowOrUpdate()]);
        }

        pane = PickerPane.Items;
        Select(Wrap(selectedIndex + 1, items.Count));
        return new GestureResult(true, [ShowOrUpdate()]);
    }

    private GestureResult OnTriggerUp()
    {
        var wasDown = triggerDown;
        triggerDown = false;
        if (active && wasDown && PasteOnTriggerRelease && !pickerVisible)
        {
            // A quick Ctrl+V: paste now, like a normal paste, even though the modifier is still held (issue #639). The
            // picker is not visible, so nothing has been chosen; once it is, the modifier's release pastes instead.
            return new GestureResult(true, Finish());
        }

        return active || wasDown ? GestureResult.Swallow : GestureResult.PassThrough;
    }

    private GestureResult OnNavigationKey(KeyInput input)
    {
        if (!active)
        {
            return GestureResult.PassThrough;
        }

        if (!input.IsDown)
        {
            return GestureResult.Swallow;
        }

        switch (input.Key)
        {
            case GestureKey.Escape:
                return new GestureResult(true, Cancel().Effects);
            case GestureKey.Enter:
                return new GestureResult(true, Finish());
            case GestureKey.Space:
                if (heldOpen)
                {
                    return GestureResult.Swallow;
                }

                // Shown right away, even before the show delay, so the user sees that it stays open.
                heldOpen = true;
                return new GestureResult(true, [ShowOrUpdate()]);
        }

        if (items.Count == 0)
        {
            return GestureResult.Swallow;
        }

        if (input.Key == GestureKey.Search)
        {
            // The picker stays open for the search, which takes over from here.
            var effects = new List<GestureEffect>(2);
            if (!pickerVisible)
            {
                effects.Add(ShowOrUpdate());
            }

            effects.Add(new BeginSearch(CreateSelection()));
            Reset();
            return new GestureResult(true, effects);
        }

        switch (input.Key)
        {
            case GestureKey.Up:
                Move(-1);
                break;
            case GestureKey.Down:
                Move(+1);
                break;
            case GestureKey.Right:
                // The actions are to the right of the history.
                if (items[selectedIndex].ActionCount > 0)
                {
                    pane = PickerPane.Actions;
                }

                break;
            case GestureKey.Left:
                pane = PickerPane.Items;
                break;
            case GestureKey.Delete:
                return new GestureResult(true, DeleteSelected());
            case >= GestureKey.Digit1 and <= GestureKey.Digit9:
                var index = input.Key - GestureKey.Digit1;
                if (index >= items.Count)
                {
                    return GestureResult.Swallow;
                }

                pane = PickerPane.Items;
                Select(index);
                break;
        }

        selectionChanged = true;
        return new GestureResult(true, [ShowOrUpdate()]);
    }

    private List<GestureEffect> DeleteSelected()
    {
        var effects = new List<GestureEffect> { new DeleteItem(items[selectedIndex].Id) };
        var deletedIndex = selectedIndex;
        items.RemoveAt(deletedIndex);
        if (deletedIndex < defaultIndex)
        {
            defaultIndex--;
        }

        if (items.Count == 0)
        {
            if (pickerVisible)
            {
                effects.Add(new HidePicker());
            }

            effects.Add(new GestureCancelled());
            Reset();
            return effects;
        }

        selectedIndex = Math.Min(deletedIndex, items.Count - 1);
        defaultIndex = Math.Min(defaultIndex, items.Count - 1);
        selectedActionIndex = 0;
        pane = PickerPane.Items;
        selectionChanged = true;
        effects.Add(ShowOrUpdate());
        return effects;
    }

    private void Move(int delta)
    {
        if (pane == PickerPane.Actions)
        {
            var count = items[selectedIndex].ActionCount;
            if (count > 0)
            {
                selectedActionIndex = Wrap(selectedActionIndex + delta, count);
            }
        }
        else
        {
            Select(Wrap(selectedIndex + delta, items.Count));
        }
    }

    private void Select(int index)
    {
        if (index != selectedIndex)
        {
            selectedActionIndex = 0;
        }

        selectedIndex = index;
        selectionChanged = true;
    }

    private void Begin(TimeSpan now)
    {
        var session = sessionFactory();
        items = [.. session.Items];
        defaultIndex = items.Count == 0 ? 0 : Math.Clamp(session.DefaultIndex, 0, items.Count - 1);
        selectedIndex = defaultIndex;
        selectedActionIndex = 0;
        pane = PickerPane.Items;
        selectionChanged = false;
        pickerVisible = false;
        startedAt = now;
        active = true;
    }

    private GestureEffect ShowOrUpdate()
    {
        var selection = CreateSelection();
        if (pickerVisible)
        {
            return new UpdatePicker(selection);
        }

        pickerVisible = true;
        return new ShowPicker(selection);
    }

    private PickerSelection CreateSelection() =>
        new(items.Select(item => item.Id).ToArray(), selectedIndex, selectedActionIndex, pane, heldOpen);

    private void Reset()
    {
        // triggerDown tracks the physical key and is only cleared by its key-up.
        active = false;
        pickerVisible = false;
        heldOpen = false;
        selectionChanged = false;
        items = [];
        selectedIndex = 0;
        defaultIndex = 0;
        selectedActionIndex = 0;
        pane = PickerPane.Items;
    }

    internal static int Wrap(int value, int count) => ((value % count) + count) % count;
}
