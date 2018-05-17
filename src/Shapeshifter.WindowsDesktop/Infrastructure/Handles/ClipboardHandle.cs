namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;
    using System.Collections.Generic;
	using System.Linq;
	using Controls.Window.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;
	using Shapeshifter.WindowsDesktop.Data.Interfaces;

	class ClipboardHandle: IClipboardHandle
    {
        readonly IClipboardNativeApi clipboardNativeApi;
		readonly IClipboardFormatFactory clipboardFormatFactory;

		public ClipboardHandle(
            IClipboardNativeApi clipboardNativeApi,
			IClipboardFormatFactory clipboardFormatFactory,
            IMainWindowHandleContainer mainWindow)
        {
            this.clipboardNativeApi = clipboardNativeApi;
			this.clipboardFormatFactory = clipboardFormatFactory;
			clipboardNativeApi.OpenClipboard(mainWindow.Handle);
        }

        public void Dispose()
        {
            clipboardNativeApi.CloseClipboard();
        }

        public IReadOnlyCollection<IClipboardFormat> GetClipboardFormats()
        {
            return clipboardNativeApi
				.GetClipboardFormats()
				.Select(clipboardFormatFactory.Create)
				.ToArray();
        }

        public IntPtr SetClipboardData(uint uFormat, IntPtr hMem)
        {
            return clipboardNativeApi.SetClipboardData(uFormat, hMem);
        }

        public bool EmptyClipboard()
        {
            return clipboardNativeApi.EmptyClipboard();
        }
    }
}