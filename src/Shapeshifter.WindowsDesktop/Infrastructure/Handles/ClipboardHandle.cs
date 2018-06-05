namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;
    using System.Collections.Generic;
	using System.Linq;
	using Controls.Window.Interfaces;

    using Interfaces;

    using Native.Interfaces;

	using Services.Interfaces;

	using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;
	using Shapeshifter.WindowsDesktop.Data.Interfaces;

	using Threading.Interfaces;

	class ClipboardHandle: IClipboardHandle
    {
        readonly IClipboardNativeApi clipboardNativeApi;
		readonly IClipboardFormatFactory clipboardFormatFactory;
		readonly ITrayIconManager trayIconManager;

		readonly bool isClipboardOpen;

		

		public ClipboardHandle(
            IClipboardNativeApi clipboardNativeApi,
			IClipboardFormatFactory clipboardFormatFactory,
			ITrayIconManager trayIconManager,
            IMainWindowHandleContainer mainWindow)
        {
            this.clipboardNativeApi = clipboardNativeApi;
			this.clipboardFormatFactory = clipboardFormatFactory;
			this.trayIconManager = trayIconManager;

			isClipboardOpen = clipboardNativeApi.OpenClipboard(mainWindow.Handle);
			if (!isClipboardOpen)
				DisplayClipboardHijackNotification();
		}

		void DisplayClipboardHijackNotification()
		{
			trayIconManager.DisplayError("Something hijacked the clipboard", "Something else is preventing Shapeshifter from using the clipboard at the moment. Try again later.");
		}

		public void Dispose()
		{
			if (!isClipboardOpen)
				return;

            if(!clipboardNativeApi.CloseClipboard())
				throw new InvalidOperationException("Could not close the clipboard.");
        }

        public IReadOnlyCollection<IClipboardFormat> GetClipboardFormats()
        {
			if(!isClipboardOpen)
				return new IClipboardFormat[0];

            return clipboardNativeApi
				.GetClipboardFormats()
				.Select(clipboardFormatFactory.Create)
				.ToArray();
        }

		public bool OpenedSuccessfully => isClipboardOpen;

		public IntPtr SetClipboardData(uint uFormat, IntPtr hMem)
		{
			if (!isClipboardOpen) 
				return IntPtr.Zero;

            return clipboardNativeApi.SetClipboardData(uFormat, hMem);
        }

        public void EmptyClipboard()
		{
			if (!isClipboardOpen)
				return;

			if(!clipboardNativeApi.EmptyClipboard())
				DisplayClipboardHijackNotification();
        }
    }
}