namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    public interface IClipboardNativeApi
    {
        uint EnumClipboardFormats(uint format);

        bool OpenClipboard(IntPtr hWndNewOwner);

        IntPtr GetClipboardData(uint uFormat);

        bool CloseClipboard();

        bool EmptyClipboard();

        int GetClipboardFormatName(
            uint format,
            [Out] StringBuilder lpszFormatName,
            int cchMaxCount);

        int DragQueryFile(
            IntPtr hDrop,
            uint iFile,
            StringBuilder lpszFile,
            int cch);

        void DragFinish(IntPtr hDrop);

        IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        IReadOnlyCollection<uint> GetClipboardFormats();

        byte[] GetClipboardDataBytes(uint format);

        string GetClipboardFormatName(uint ClipboardFormat);

        bool AddClipboardFormatListener(IntPtr hwnd);

        IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        IntPtr GetClipboardOwner();

        bool RemoveClipboardFormatListener(IntPtr hwnd);

        uint GetClipboardSequenceNumber();
    }
}