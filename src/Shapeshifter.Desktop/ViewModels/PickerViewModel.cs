using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>State of the picker window. Driven entirely by the engine; the window never takes focus.</summary>
public sealed partial class PickerViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItem))]
    private int selectedIndex = -1;

    /// <summary>
    /// Whether the preview of the selected item is shown above its actions (issue #54). The presenter hides it on
    /// screens too short for the taller picker.
    /// </summary>
    [ObservableProperty]
    private bool showsPreview = true;

    [ObservableProperty]
    private int selectedActionIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemsPaneActive))]
    private bool isActionsPaneActive;

    /// <summary>The user pressed Space, so the picker stays open after the modifier is released (issue #420).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReleaseHint), nameof(HeaderHint), nameof(IsNotHeldOpen), nameof(EscapeHint))]
    private bool isHeldOpen;

    /// <summary>Height available to the history on the current screen, in window units.</summary>
    [ObservableProperty]
    private double maxListHeight = 600;

    /// <summary>
    /// True when the window cannot be see-through (no compositor): the panel then fills the window with square
    /// corners and no shadow, rather than leaving opaque black corners around it.
    /// </summary>
    [ObservableProperty]
    private bool isOpaque = true;

    /// <summary>"5 items", in the header.</summary>
    [ObservableProperty]
    private string itemCountText = string.Empty;

    /// <summary>"Actions for this text", above the actions.</summary>
    [ObservableProperty]
    private string actionsHeader = Strings.PickerActions;

    /// <summary>The picker has the keyboard focus and shows the search box (issue #458).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(HasNoMatches), nameof(IsNotSearching), nameof(HeaderHint), nameof(ShowsActions))]
    private bool isSearching;

    /// <summary>What the user typed in the search box.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoMatchesText))]
    private string searchQuery = string.Empty;

    public PickerViewModel()
        : this("Ctrl")
    {
    }

    /// <param name="modifierName">The key held to keep the picker open: "Ctrl", "Cmd" on macOS, or "Win".</param>
    public PickerViewModel(string modifierName)
    {
        SetModifierName(modifierName);
        Items.CollectionChanged += (_, _) =>
        {
            ItemCountText = Items.Count == 1 ? Strings.PickerItemCountOne : Localizer.Format(Strings.PickerItemCountOther, Items.Count);
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasNoMatches));
            OnPropertyChanged(nameof(ShowsActions));
            OnPropertyChanged(nameof(SelectedItem));
        };
    }

    /// <summary>The key cap of the modifier in the footer: "Ctrl", "⌘" or "Win".</summary>
    public string ModifierKey { get; private set; } = string.Empty;

    /// <summary>Follows a change of the paste shortcut in the settings.</summary>
    public void SetModifierName(string modifierName)
    {
        ArgumentNullException.ThrowIfNull(modifierName);
        ModifierKey = modifierName == "Cmd" ? "⌘" : Localizer.ModifierKey(modifierName);
        EmptyHint = CreateEmptyHint(Localizer.ModifierKey(modifierName));
        OnPropertyChanged(nameof(ModifierKey));
        OnPropertyChanged(nameof(ReleaseHint));
        OnPropertyChanged(nameof(HeaderHint));
        OnPropertyChanged(nameof(EmptyHint));
    }

    /// <summary>"Release Ctrl to paste", or "Press Enter to paste" while the picker is kept open.</summary>
    public string ReleaseHint => IsHeldOpen ? Strings.PickerReleaseHintHeldOpen : Localizer.Format(Strings.PickerReleaseHint, ModifierKey);

    public bool IsNotHeldOpen => !IsHeldOpen;

    /// <summary>What Esc does: cancel the paste, or close a picker that is kept open.</summary>
    public string EscapeHint => IsHeldOpen ? Strings.PickerHintClose : Strings.PickerHintCancel;

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    /// <summary>The item the preview pane shows.</summary>
    public PickerItemViewModel? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    public bool HasItems => Items.Count > 0;

    /// <summary>Nothing has been copied yet, so the picker explains how to use it (issue #144).</summary>
    public bool IsEmpty => Items.Count == 0 && !IsSearching;

    /// <summary>The search found nothing.</summary>
    public bool HasNoMatches => Items.Count == 0 && IsSearching;

    public bool IsNotSearching => !IsSearching;

    /// <summary>
    /// The actions pane is shown beside the history, and stays while a search finds nothing, so the picker keeps its width.
    /// </summary>
    public bool ShowsActions => HasItems || IsSearching;

    public string NoMatchesText => Localizer.Format(Strings.PickerSearchNoMatchesText, SearchQuery.Trim());

    /// <summary>The text in the top right corner of the header.</summary>
    public string HeaderHint => IsSearching ? Strings.PickerReleaseHintHeldOpen : ReleaseHint;

    /// <summary>How to use Shapeshifter, shown in place of the history while it is empty.</summary>
    public string EmptyHint { get; private set; } = string.Empty;

    public static string CreateEmptyHint(string modifier) =>
        Localizer.Format(Strings.PickerEmptyHint, modifier) + Strings.PickerEmptyHintHoldOpen;

    public ObservableCollection<PickerActionViewModel> Actions { get; } = [];

    public bool IsItemsPaneActive => !IsActionsPaneActive;

    partial void OnSelectedIndexChanged(int value) => UpdateActionsHeader();

    /// <summary>
    /// Selects the item and the action, or none (-1) while there are no items or no actions. A list cannot select an
    /// index it does not have and then shows nothing, without telling the binding: the index here would no longer match
    /// what the list shows, and setting that same index again once the list has items would change nothing, so no item
    /// would be selected.
    /// </summary>
    public void Select(int index, int actionIndex)
    {
        SelectedIndex = Items.Count == 0 ? -1 : index;
        SelectedActionIndex = Actions.Count == 0 ? -1 : actionIndex;
    }

    /// <summary>Names the kind of the selected item above its actions.</summary>
    public void UpdateActionsHeader() => ActionsHeader = SelectedIndex >= 0 && SelectedIndex < Items.Count
        ? Items[SelectedIndex].Layout switch
        {
            PickerItemLayout.Image => Strings.PickerActionsForImage,
            PickerItemLayout.Files => Strings.PickerActionsForFiles,
            PickerItemLayout.File => Strings.PickerActionsForFile,
            _ => Strings.PickerActionsForText,
        }
        : Strings.PickerActions;
}
