using Shapeshifter.WindowsDesktop.Controls.Setup.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels;

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
