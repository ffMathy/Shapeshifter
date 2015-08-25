using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Api
{
    static class ClipboardApi
    {

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

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

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
            return GlobalSize(dataPointer);
        }

        static IntPtr GetLockedMemoryBlockPointer(IntPtr dataPointer)
        {
            return GlobalLock(dataPointer);
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
