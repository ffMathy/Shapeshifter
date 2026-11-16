using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Shapeshifter.Desktop.ViewModels;

/// <summary>State of the picker window. Driven entirely by the engine; the window never takes focus.</summary>
public sealed partial class PickerViewModel : ObservableObject
{
    [ObservableProperty]
    private int selectedIndex;

    [ObservableProperty]
    private int selectedActionIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemsPaneActive))]
    private bool isActionsPaneActive;

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
        };
    }

    /// <summary>The key cap of the modifier in the footer: "Ctrl" or "⌘".</summary>
    public string ModifierKey { get; }

    /// <summary>"Release Ctrl to paste".</summary>
    public string ReleaseHint => $"Release {ModifierKey} to paste";

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    /// <summary>Nothing has been copied yet, so the picker explains how to use it (issue #144).</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>How to use Shapeshifter, shown in place of the history while it is empty.</summary>
    public string EmptyHint { get; }

    public static string CreateEmptyHint(string modifier) =>
        "Copy as usual, and Shapeshifter keeps everything you copy.\n\n" +
        $"To paste something you copied earlier, hold {modifier} and press V. Keep holding {modifier} to see the history, " +
        $"press V or the arrow keys to choose, and release {modifier} to paste.";

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
