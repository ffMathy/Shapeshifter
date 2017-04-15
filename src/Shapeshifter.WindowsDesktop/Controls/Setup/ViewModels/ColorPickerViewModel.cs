using Shapeshifter.WindowsDesktop.Controls.Setup.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;

namespace Shapeshifter.WindowsDesktop.Controls.Setup.ViewModels
{
    class ColorPickerViewModel : IColorPickerViewModel
    {
        public IUserInterfaceViewModel UserInterfaceViewModel { get; private set; }

        public ColorPickerViewModel(
            IUserInterfaceViewModel userInterfaceViewModel)
        {
            UserInterfaceViewModel = userInterfaceViewModel;
        }
    }
}
