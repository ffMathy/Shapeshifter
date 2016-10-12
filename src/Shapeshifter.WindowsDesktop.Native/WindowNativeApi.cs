namespace Shapeshifter.WindowsDesktop.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    using Interfaces;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class WindowNativeApi: IWindowNativeApi
    {
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
            return GetWindowText(hWnd, text, count);
        }

        string IWindowNativeApi.GetWindowTitle(IntPtr windowHandle)
        {
            return GetWindowTitle(windowHandle);
        }

        IntPtr IWindowNativeApi.LoadIcon(IntPtr hInstance, IntPtr lpIconName)
        {
            return LoadIcon(hInstance, lpIconName);
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
        internal static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

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

        IntPtr IWindowNativeApi.GetWindowThreadProcessId(IntPtr hWnd, out uint processId)
        {
            return GetWindowThreadProcessId(hWnd, out processId);
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

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

        public static IntPtr ICON_BIG => new IntPtr(1);

        IntPtr IWindowNativeApi.ICON_BIG => ICON_BIG;

        public static IntPtr IDI_APPLICATION => new IntPtr(0x7F00);

        public const int GCL_HICON = -14;

        internal static string GetWindowTitle(IntPtr windowHandle)
        {
            const int numberOfCharacters = 512;
            var buffer = new StringBuilder(numberOfCharacters);

            if (GetWindowText(windowHandle, buffer, numberOfCharacters) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }
    }
}