using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardTextDataControlFactory : IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>
    {
        public IClipboardTextDataControl CreateControl(IClipboardTextData data)
        {
            return new ClipboardTextDataControl()
            {
                DataContext = new ClipboardTextDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
