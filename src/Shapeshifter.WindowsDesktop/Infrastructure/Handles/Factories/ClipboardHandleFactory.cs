namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Controls.Window.Interfaces;

    using Handles.Interfaces;

    using Interfaces;

    using Native.Interfaces;

	using Services.Interfaces;

	using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;

	class ClipboardHandleFactory: IClipboardHandleFactory
    {
        readonly IMainWindowHandleContainer mainWindow;
        readonly IClipboardNativeApi clipboardNativeApi;
		readonly IClipboardFormatFactory clipboardFormatFactory;
		readonly ITrayIconManager trayIconManager;

		public ClipboardHandleFactory(
            IMainWindowHandleContainer mainWindow,
            IClipboardNativeApi clipboardNativeApi,
			IClipboardFormatFactory clipboardFormatFactory,
			ITrayIconManager trayIconManager)
        {
            this.mainWindow = mainWindow;
            this.clipboardNativeApi = clipboardNativeApi;
			this.clipboardFormatFactory = clipboardFormatFactory;
			this.trayIconManager = trayIconManager;
		}

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(
                clipboardNativeApi,
				clipboardFormatFactory,
				trayIconManager,
				mainWindow);
        }
    }
}