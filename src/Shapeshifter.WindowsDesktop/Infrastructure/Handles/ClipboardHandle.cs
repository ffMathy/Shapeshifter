namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;
    using System.Collections.Generic;

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

        public IReadOnlyCollection<uint> GetClipboardFormats()
        {
            return clipboardNativeApi.GetClipboardFormats();
        }

        public IntPtr SetClipboardData(uint uFormat, IntPtr hMem)
        {
            return clipboardNativeApi.SetClipboardData(uFormat, hMem);
        }

        public void EmptyClipboard()
        {
            clipboardNativeApi.EmptyClipboard();
        }
    }
}