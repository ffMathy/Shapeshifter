namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Controls.Window.Interfaces;

    using Handles.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    class ClipboardHandleFactory: IClipboardHandleFactory
    {
        readonly IMainWindowHandleContainer mainWindow;

        readonly IClipboardNativeApi clipboardNativeApi;

        public ClipboardHandleFactory(
            IMainWindowHandleContainer mainWindow,
            IClipboardNativeApi clipboardNativeApi)
        {
            this.mainWindow = mainWindow;
            this.clipboardNativeApi = clipboardNativeApi;
        }

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(
                clipboardNativeApi,
                mainWindow);
        }
    }
}