using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories
{
    class ClipboardHandleFactory : IClipboardHandleFactory
    {
        readonly IMainWindowHandleContainer mainWindow;

        public ClipboardHandleFactory(
            IMainWindowHandleContainer mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(mainWindow);
        }
    }
}
