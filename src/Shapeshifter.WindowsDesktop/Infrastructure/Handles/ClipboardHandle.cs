namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using Api;

    using Controls.Window.Interfaces;

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