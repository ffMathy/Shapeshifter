namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using Controls.Window.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    class ClipboardHandle: IClipboardHandle
    {
        readonly IClipboardNativeApi clipboardNativeApi;

        public ClipboardHandle(
            IClipboardNativeApi clipboardNativeApi,
            IMainWindowHandleContainer mainWindow)
        {
            this.clipboardNativeApi = clipboardNativeApi;
            clipboardNativeApi.OpenClipboard(mainWindow.Handle);
        }

        public void Dispose()
        {
            clipboardNativeApi.CloseClipboard();
        }
    }
}