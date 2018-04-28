namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Controls.Window.Interfaces;

    using Handles.Interfaces;

    using Interfaces;

    using Native.Interfaces;
	using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;

	class ClipboardHandleFactory: IClipboardHandleFactory
    {
        readonly IMainWindowHandleContainer mainWindow;
        readonly IClipboardNativeApi clipboardNativeApi;
		readonly IClipboardFormatFactory clipboardFormatFactory;

		public ClipboardHandleFactory(
            IMainWindowHandleContainer mainWindow,
            IClipboardNativeApi clipboardNativeApi,
			IClipboardFormatFactory clipboardFormatFactory)
        {
            this.mainWindow = mainWindow;
            this.clipboardNativeApi = clipboardNativeApi;
			this.clipboardFormatFactory = clipboardFormatFactory;
		}

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(
                clipboardNativeApi,
				clipboardFormatFactory,
                mainWindow);
        }
    }
}