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
    /// True when the window cannot be see-through (no compositor), so the actions pane fills its column rather than
    /// leaving an opaque black area below it.
    /// </summary>
    [ObservableProperty]
    private bool actionsFillHeight = true;

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    public ObservableCollection<PickerActionView> Actions { get; } = [];

    public bool IsItemsPaneActive => !IsActionsPaneActive;
}
