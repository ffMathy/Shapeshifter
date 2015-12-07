namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    using Windows.Interfaces;

    using Api;

    using Interfaces;

    class ClipboardHandle: IClipboardHandle
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