using Shapeshifter.UserInterface.WindowsDesktop.Api;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Api
{
    public static class ClipboardApi
    {

        public const int CF_BITMAP = 2;
        public const int CF_DIB = 8;
        public const int CF_DIBV5 = 17;
        public const int CF_DIF = 5;
        public const int CF_DSPBITMAP = 0x0082;
        public const int CF_DSPENHMETAFILE = 0x008E;
        public const int CF_DSPMETAFILEPICT = 0x0083;
        public const int CF_DSPTEXT = 0x0081;
        public const int CF_ENHMETAFILE = 14;
        public const int CF_GDIOBJFIRST = 0x0300;
        public const int CF_GDIOBJLAST = 0x03FF;
        public const int CF_HDROP = 15;
        public const int CF_LOCALE = 16;
        public const int CF_METAFILEPICT = 3;
        public const int CF_OEMTEXT = 7;
        public const int CF_OWNERDISPLAY = 0x0080;
        public const int CF_PALETTE = 9;
        public const int CF_PENDATA = 10;
        public const int CF_PRIVATEFIRST = 0x0200;
        public const int CF_PRIVATELAST = 0x02FF;
        public const int CF_RIFF = 11;
        public const int CF_SYLK = 4;
        public const int CF_TEXT = 1;
        public const int CF_TIFF = 6;
        public const int CF_UNICODETEXT = 13;
        public const int CF_WAVE = 12;

        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_CHANGECBCHAIN = 0x0003;
        public const int WM_DRAWCLIPBOARD = 0x0308;

        [DllImport("user32.dll")]
        public static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll")]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        public static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern int DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        public static IEnumerable<uint> GetClipboardFormats()
        {
            var lastRetrievedFormat = 0u;
            while (0 != (lastRetrievedFormat = EnumClipboardFormats(lastRetrievedFormat)))
            {
                yield return lastRetrievedFormat;
            }
        }

        //TODO: refactor this into custom service.
        public static byte[] GetClipboardDataBytes(uint format)
        {
            var dataPointer = GetClipboardDataPointer(format);

            var length = GetPointerDataLength(dataPointer);
            var lockedMemory = GetLockedMemoryBlockPointer(dataPointer);

            var buffer = new byte[(int)length];

            Marshal.Copy(lockedMemory, buffer, 0, (int)length);
            
            return buffer;
        }
        
        static IntPtr GetClipboardDataPointer(uint format)
        {
            return GetClipboardData(format);
        }

        static UIntPtr GetPointerDataLength(IntPtr dataPointer)
        {
            return GeneralApi.GlobalSize(dataPointer);
        }

        static IntPtr GetLockedMemoryBlockPointer(IntPtr dataPointer)
        {
            return GeneralApi.GlobalLock(dataPointer);
        }

        public static string GetClipboardFormatName(uint ClipboardFormat)
        {
            StringBuilder sb = new StringBuilder(512);
            GetClipboardFormatName(ClipboardFormat, sb, sb.Capacity);
            return sb.ToString();
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardOwner();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern uint GetClipboardSequenceNumber();
    }
}
