using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Shapeshifter.Core.Input;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class PickerWindow : Window
{
    public PickerWindow()
    {
        InitializeComponent();
        NativeMaterial.Prepare(this, Chrome);

        // Before the search box sees them, so the arrows, Enter and Escape drive the picker rather than the text.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// While searching: what the user pressed; returns whether the picker used it (the search box then ignores it).
    /// <see langword="null"/> while not searching.
    /// </summary>
    internal Func<PickerSearchKey, bool>? SearchKeyHandler { get; set; }

    /// <summary>Gives the search box the keyboard focus, with the caret at the end.</summary>
    internal bool FocusSearchBox()
    {
        SearchBox.CaretIndex = SearchBox.Text?.Length ?? 0;
        return SearchBox.Focus(NavigationMethod.Tab);
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (SearchKeyHandler is not { } handler || e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift))
        {
            return;
        }

        PickerSearchKey? key = e.Key switch
        {
            Key.Up => PickerSearchKey.Up,
            Key.Down => PickerSearchKey.Down,
            Key.Left => PickerSearchKey.Left,

            // Right moves the caret until it reaches the end of the text; then it opens the actions.
            Key.Right when SearchBox.CaretIndex >= (SearchBox.Text?.Length ?? 0) && SearchBox.SelectionStart == SearchBox.SelectionEnd => PickerSearchKey.Right,
            Key.Enter => PickerSearchKey.Enter,
            Key.Escape => PickerSearchKey.Escape,
            _ => null,
        };

        if (key is { } searchKey && handler(searchKey))
        {
            e.Handled = true;
        }
    }
}
