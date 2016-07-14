namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;
    using System.Text;

    public interface IWindowNativeApi
    {
        IntPtr ICON_BIG { get; }

        IntPtr IDI_APPLICATION { get; }

        uint GetClassLong32(IntPtr hWnd, int nIndex);

        IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        IntPtr GetForegroundWindow();

        int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        string GetWindowTitle(IntPtr windowHandle);

        IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WindowNativeApi.WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        bool UnhookWinEvent(IntPtr hWinEventHook);

        IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}