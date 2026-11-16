using Avalonia.Controls;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        NativeMaterial.PrepareBackdrop(this);
    }
}
