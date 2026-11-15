using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private string hint = string.Empty;

    public ObservableCollection<PickerItemViewModel> Items { get; } = [];

    public ObservableCollection<string> Actions { get; } = [];

    public bool IsItemsPaneActive => !IsActionsPaneActive;
}
