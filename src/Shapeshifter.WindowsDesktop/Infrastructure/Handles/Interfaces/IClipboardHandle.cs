namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Interfaces
{
	using Shapeshifter.WindowsDesktop.Data.Interfaces;
	using System;
    using System.Collections.Generic;

    public interface IClipboardHandle: IHandle
    {
        IReadOnlyCollection<IClipboardFormat> GetClipboardFormats();

        IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        void EmptyClipboard();
    }
}