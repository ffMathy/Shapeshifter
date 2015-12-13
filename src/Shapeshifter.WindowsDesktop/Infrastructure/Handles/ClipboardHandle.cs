namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using Controls.Window.Interfaces;

    using Interfaces;

    using Native;

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