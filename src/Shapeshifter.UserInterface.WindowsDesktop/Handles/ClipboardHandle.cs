using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    class ClipboardHandle : IClipboardHandle
    {
        public ClipboardHandle(
            IClipboardListWindow mainWindow)
        {
            ClipboardApi.OpenClipboard(mainWindow.HandleSource.Handle);
        }

        public void Dispose()
        {
            ClipboardApi.CloseClipboard();
        }
    }
}
