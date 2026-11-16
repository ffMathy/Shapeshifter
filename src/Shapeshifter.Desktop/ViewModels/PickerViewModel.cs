using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shapeshifter.Core.Engine;

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
    /// True when the window cannot be see-through (no compositor), so both panes fill the window height rather than
    /// leaving an opaque black area below the shorter one.
    /// </summary>
    [ObservableProperty]
    private bool actionsFillHeight = true;

    public PickerViewModel()
    {
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    /// <summary>Nothing has been copied yet, so the picker explains how to use it (issue #144).</summary>
    public bool IsEmpty => Items.Count == 0;

    public static string EmptyHint { get; } = CreateEmptyHint(OperatingSystem.IsMacOS() ? "Cmd" : "Ctrl");

    public static string CreateEmptyHint(string modifier) =>
        "Copy as usual, and Shapeshifter keeps everything you copy.\n\n" +
        $"To paste something you copied earlier, hold {modifier} and press V. Keep holding {modifier} to see the history, " +
        $"press V or the arrow keys to choose, and release {modifier} to paste.";

    public ObservableCollection<PickerActionView> Actions { get; } = [];

    public bool IsItemsPaneActive => !IsActionsPaneActive;
}
