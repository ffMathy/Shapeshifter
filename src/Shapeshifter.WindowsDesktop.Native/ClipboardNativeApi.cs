namespace Shapeshifter.WindowsDesktop.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;

    using Interfaces;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ClipboardNativeApi: IClipboardNativeApi
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

        [DllImport("user32.dll")]
        internal static extern uint EnumClipboardFormats(uint format);

        bool IClipboardNativeApi.OpenClipboard(IntPtr hWndNewOwner)
        {
            return OpenClipboard(hWndNewOwner);
        }

        IntPtr IClipboardNativeApi.GetClipboardData(uint uFormat)
        {
            return GetClipboardData(uFormat);
        }

        bool IClipboardNativeApi.CloseClipboard()
        {
            return CloseClipboard();
        }

        bool IClipboardNativeApi.EmptyClipboard()
        {
            return EmptyClipboard();
        }

        int IClipboardNativeApi.GetClipboardFormatName(uint format, StringBuilder lpszFormatName, int cchMaxCount)
        {
            return GetClipboardFormatName(format, lpszFormatName, cchMaxCount);
        }

        int IClipboardNativeApi.DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch)
        {
            return DragQueryFile(hDrop, iFile, lpszFile, cch);
        }

        void IClipboardNativeApi.DragFinish(IntPtr hDrop)
        {
            DragFinish(hDrop);
        }

        IntPtr IClipboardNativeApi.SetClipboardData(uint uFormat, IntPtr hMem)
        {
            return SetClipboardData(uFormat, hMem);
        }

        IReadOnlyCollection<uint> IClipboardNativeApi.GetClipboardFormats()
        {
            return GetClipboardFormats();
        }

        byte[] IClipboardNativeApi.GetClipboardDataBytes(uint format)
        {
            return GetClipboardDataBytes(format);
        }

        string IClipboardNativeApi.GetClipboardFormatName(uint ClipboardFormat)
        {
            return GetClipboardFormatName(ClipboardFormat);
        }

        bool IClipboardNativeApi.AddClipboardFormatListener(IntPtr hwnd)
        {
            return AddClipboardFormatListener(hwnd);
        }

        IntPtr IClipboardNativeApi.SetClipboardViewer(IntPtr hWndNewViewer)
        {
            return SetClipboardViewer(hWndNewViewer);
        }

        bool IClipboardNativeApi.ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext)
        {
            return ChangeClipboardChain(hWndRemove, hWndNewNext);
        }

        int IClipboardNativeApi.SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hWnd, Msg, wParam, lParam);
        }

        IntPtr IClipboardNativeApi.GetClipboardOwner()
        {
            return GetClipboardOwner();
        }

        bool IClipboardNativeApi.RemoveClipboardFormatListener(IntPtr hwnd)
        {
            return RemoveClipboardFormatListener(hwnd);
        }

        uint IClipboardNativeApi.GetClipboardSequenceNumber()
        {
            return GetClipboardSequenceNumber();
        }

        uint IClipboardNativeApi.EnumClipboardFormats(uint format)
        {
            return EnumClipboardFormats(format);
        }

        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        internal static extern int GetClipboardFormatName(
            uint format,
            [Out] StringBuilder lpszFormatName,
            int cchMaxCount);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int DragQueryFile(
            IntPtr hDrop,
            uint iFile,
            StringBuilder lpszFile,
            int cch);

        [DllImport("shell32.dll")]
        internal static extern void DragFinish(IntPtr hDrop);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        internal static IReadOnlyCollection<uint> GetClipboardFormats()
        {
            var formats = new List<uint>();
            var lastRetrievedFormat = 0u;
            while (0 != (lastRetrievedFormat = EnumClipboardFormats(lastRetrievedFormat)))
            {
				switch(lastRetrievedFormat) {
					case CF_BITMAP:
						formats.Add(CF_DIB);
						formats.Add(CF_DIBV5);
						break;
					
					case CF_DIB:
						formats.Add(CF_BITMAP);
						formats.Add(CF_PALETTE);
						formats.Add(CF_DIBV5);
						break;

					case CF_DIBV5:
						formats.Add(CF_BITMAP);
						formats.Add(CF_DIB);
						formats.Add(CF_PALETTE);
						break;

					case CF_ENHMETAFILE:
						formats.Add(CF_METAFILEPICT);
						break;

					case CF_METAFILEPICT:
						formats.Add(CF_ENHMETAFILE);
						break;
				}

				if(!formats.Contains(lastRetrievedFormat))
					formats.Add(lastRetrievedFormat);
            }

            return formats;
        }
		
        internal static byte[] GetClipboardDataBytes(uint format)
        {
            var dataPointer = GetClipboardDataPointer(format);

            var length = GetPointerDataLength(dataPointer);
            if (length == UIntPtr.Zero)
            {
                return null;
            }

            var lockedMemory = GetLockedMemoryBlockPointer(dataPointer);
            if (lockedMemory == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var buffer = new byte[(int) length];
            Marshal.Copy(lockedMemory, buffer, 0, (int) length);
            GeneralNativeApi.GlobalUnlock(dataPointer);

            return buffer;
        }

        static IntPtr GetClipboardDataPointer(uint format)
        {
            return GetClipboardData(format);
        }

        static UIntPtr GetPointerDataLength(IntPtr dataPointer)
        {
            return GeneralNativeApi.GlobalSize(dataPointer);
        }

        static IntPtr GetLockedMemoryBlockPointer(IntPtr dataPointer)
        {
            return GeneralNativeApi.GlobalLock(dataPointer);
        }

        internal static string GetClipboardFormatName(uint ClipboardFormat)
        {
            var sb = new StringBuilder(512);
            GetClipboardFormatName(ClipboardFormat, sb, sb.Capacity);

			var formatName = sb.ToString().Trim();
			if(formatName.Contains("\0"))
				formatName = formatName.Substring(0, formatName.IndexOf("\0"));

			return formatName;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardOwner();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern uint GetClipboardSequenceNumber();
    }
}