namespace Shapeshifter.WindowsDesktop.Native
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.InteropServices;
	using System.Text;

	using Interfaces;

	[ExcludeFromCodeCoverage]
    public class WindowNativeApi: IWindowNativeApi
    {
		public struct WINDOWINFO
		{
			public uint ownerpid;
			public uint childpid;
		}

        IntPtr IWindowNativeApi.GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            return GetClassLongPtr(hWnd, nIndex);
        }

        IntPtr IWindowNativeApi.GetForegroundWindow()
        {
            return GetForegroundWindow();
        }

        int IWindowNativeApi.GetWindowText(IntPtr hWnd, StringBuilder text, int count)
        {
            return GetWindowTextW(hWnd, text, count);
        }

        string IWindowNativeApi.GetWindowTitle(IntPtr windowHandle)
        {
            return GetWindowTitle(windowHandle);
        }

        IntPtr IWindowNativeApi.LoadIcon(IntPtr hInstance, IntPtr lpIconName)
        {
            return LoadIcon(hInstance, lpIconName);
        }

		bool IWindowNativeApi.EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam)
		{
			return EnumChildWindows(hWndParent, lpEnumFunc, lParam);
		}

		IntPtr IWindowNativeApi.SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hWnd, Msg, wParam, lParam);
        }

        bool IWindowNativeApi.UnhookWinEvent(IntPtr hWinEventHook)
        {
            return UnhookWinEvent(hWinEventHook);
        }

        [DllImport("user32.dll")] 
		static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint procid);

		IntPtr IWindowNativeApi.AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach)
		{
			return AttachThreadInput(idAttach, idAttachTo, fAttach);
		}

		[DllImport("user32.dll")]
		static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        IntPtr IWindowNativeApi.SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags)
        {
            return SetWinEventHook(eventMin, eventMax, hmodWinEventProc, lpfnWinEventProc, idProcess, idThread, dwFlags);
        }

        int IWindowNativeApi.GetWindowThreadProcessId(IntPtr hWnd, out uint procid)
        {
            return GetWindowThreadProcessId(hWnd, out procid);
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint EVENT_SYSTEM_FOREGROUND = 3;

		public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", EntryPoint = "GetWindowTextW")]
		public static extern int GetWindowTextW([In]
			IntPtr hWnd, [Out, MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        IntPtr IWindowNativeApi.IDI_APPLICATION => IDI_APPLICATION;

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        internal static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

        IntPtr IWindowNativeApi.GetClassLong64(IntPtr hWnd, int nIndex)
        {
            return GetClassLong64(hWnd, nIndex);
        }

        uint IWindowNativeApi.GetClassLong32(IntPtr hWnd, int nIndex)
        {
            return GetClassLong32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        internal static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        internal static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(GetClassLong32(hWnd, nIndex));
            }
            return GetClassLong64(hWnd, nIndex);
        }

        public static IntPtr ICON_LARGE => new IntPtr(1);
        public static IntPtr ICON_SMALL => new IntPtr(0);
       
        IntPtr IWindowNativeApi.ICON_LARGE => ICON_LARGE;
        IntPtr IWindowNativeApi.ICON_SMALL => ICON_SMALL;

        public static IntPtr IDI_APPLICATION => new IntPtr(0x7F00);

        public const int GCL_HICON = -14;

        internal static string GetWindowTitle(IntPtr windowHandle)
        {
            const int numberOfCharacters = 512;
            var buffer = new StringBuilder(numberOfCharacters);

            if (GetWindowTextW(windowHandle, buffer, numberOfCharacters) > 0)
                return buffer
					.ToString()
					.Replace(char.ConvertFromUtf32(8206), "");

            return null;
        }
    }
}