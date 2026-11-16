using Avalonia.Controls;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class PickerWindow : Window
{
    public PickerWindow()
    {
        InitializeComponent();
        NativeMaterial.Prepare(this, Chrome);
    }
}
