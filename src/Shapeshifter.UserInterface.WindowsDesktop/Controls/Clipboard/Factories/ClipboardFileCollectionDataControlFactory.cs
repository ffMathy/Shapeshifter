using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardFileCollectionDataControlFactory
        : IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
    {
        public IClipboardFileCollectionDataControl CreateControl(IClipboardFileCollectionData data)
        {
            return new ClipboardFileCollectionDataControl()
            {
                DataContext = new ClipboardFileCollectionDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
