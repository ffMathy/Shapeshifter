using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    internal class ClipboardHandle : IClipboardHandle
    {
        public ClipboardHandle(
            IMainWindowHandleContainer mainWindow)
        {
            ClipboardApi.OpenClipboard(mainWindow.Handle);
        }

        public void Dispose()
        {
            ClipboardApi.CloseClipboard();
        }
    }
}