using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    class ClipboardHandle : IClipboardHandle
    {
        public ClipboardHandle(
            IWindowMessageHook windowMessageHook)
        {
            ClipboardApi.OpenClipboard(windowMessageHook.MainWindowHandle);
        }

        public void Dispose()
        {
            ClipboardApi.CloseClipboard();
        }
    }
}
