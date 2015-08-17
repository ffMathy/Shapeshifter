using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardFileDataControlFactory
        : IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
    {
        public IClipboardFileDataControl CreateControl(IClipboardFileData data)
        {
            return new ClipboardFileDataControl()
            {
                DataContext = new ClipboardFileDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
