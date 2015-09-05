using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class ClipboardHandleFactory : IClipboardHandleFactory
    {
        readonly IClipboardListWindow mainWindow;

        public ClipboardHandleFactory(
            IClipboardListWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(mainWindow);
        }
    }
}
