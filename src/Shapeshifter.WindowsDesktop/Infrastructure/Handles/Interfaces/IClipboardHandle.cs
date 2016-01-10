namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Interfaces
{
    using System;
    using System.Collections.Generic;

    public interface IClipboardHandle: IHandle
    {
        IReadOnlyCollection<uint> GetClipboardFormats();

        IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        void EmptyClipboard();
    }
}