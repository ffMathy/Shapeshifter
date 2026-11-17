using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>State of the picker window. Driven entirely by the engine; the window never takes focus.</summary>
public sealed partial class PickerViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItem))]
    private int selectedIndex;

    /// <summary>
    /// Whether the preview of the selected item is shown above its actions (issue #54). The presenter hides it on
    /// screens too short for the taller picker.
    /// </summary>
    [ObservableProperty]
    private bool showsPreview = true;

    [ObservableProperty]
    private int selectedActionIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemsPaneActive))]
    private bool isActionsPaneActive;

    /// <summary>The user pressed Space, so the picker stays open after the modifier is released (issue #420).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReleaseHint), nameof(IsNotHeldOpen), nameof(EscapeHint))]
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
    private string actionsHeader = "Actions";

    public PickerViewModel()
        : this("Ctrl")
    {
    }

    /// <param name="modifierName">The key held to keep the picker open: "Ctrl", or "Cmd" on macOS.</param>
    public PickerViewModel(string modifierName)
    {
        ArgumentNullException.ThrowIfNull(modifierName);
        ModifierKey = modifierName == "Cmd" ? "⌘" : modifierName;
        EmptyHint = CreateEmptyHint(modifierName);
        Items.CollectionChanged += (_, _) =>
        {
            ItemCountText = Items.Count == 1 ? "1 item" : string.Create(CultureInfo.CurrentCulture, $"{Items.Count} items");
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SelectedItem));
        };
    }

    /// <summary>The key cap of the modifier in the footer: "Ctrl" or "⌘".</summary>
    public string ModifierKey { get; }

    /// <summary>"Release Ctrl to paste", or "Press Enter to paste" while the picker is kept open.</summary>
    public string ReleaseHint => IsHeldOpen ? "Press Enter to paste" : $"Release {ModifierKey} to paste";

    public bool IsNotHeldOpen => !IsHeldOpen;

    /// <summary>What Esc does: cancel the paste, or close a picker that is kept open.</summary>
    public string EscapeHint => IsHeldOpen ? "Close" : "Cancel";

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    /// <summary>The item the preview pane shows.</summary>
    public PickerItemViewModel? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    public bool HasItems => Items.Count > 0;

    /// <summary>Nothing has been copied yet, so the picker explains how to use it (issue #144).</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>How to use Shapeshifter, shown in place of the history while it is empty.</summary>
    public string EmptyHint { get; }

    public static string CreateEmptyHint(string modifier) =>
        "Copy as usual, and Shapeshifter keeps everything you copy.\n\n" +
        $"To paste something you copied earlier, hold {modifier} and press V. Keep holding {modifier} to see the history, " +
        $"press V or the arrow keys to choose, and release {modifier} to paste. Press Space to keep the history open " +
        "without holding the key, then Enter to paste.";

    public ObservableCollection<PickerActionViewModel> Actions { get; } = [];

    public bool IsItemsPaneActive => !IsActionsPaneActive;

    partial void OnSelectedIndexChanged(int value) => UpdateActionsHeader();

    /// <summary>Names the kind of the selected item above its actions.</summary>
    public void UpdateActionsHeader() => ActionsHeader = SelectedIndex >= 0 && SelectedIndex < Items.Count
        ? Items[SelectedIndex].Layout switch
        {
            PickerItemLayout.Image => "Actions for this image",
            PickerItemLayout.Files => "Actions for these files",
            PickerItemLayout.File => "Actions for this file",
            _ => "Actions for this text",
        }
        : "Actions";
}
