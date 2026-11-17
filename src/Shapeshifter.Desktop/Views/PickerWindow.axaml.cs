using Avalonia.Controls;
using Shapeshifter.Core.Localization;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class PickerWindow : Window
{
    public PickerWindow()
    {
        InitializeComponent();
        if (Localizer.IsRightToLeft)
        {
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft;
        }

        NativeMaterial.Prepare(this, Chrome);
    }
}
