namespace Shapeshifter.WindowsDesktop.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    static class WindowApi
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        internal static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

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

        internal static IntPtr ICON_BIG => new IntPtr(1);

        internal static IntPtr IDI_APPLICATION => new IntPtr(0x7F00);

        public const int GCL_HICON = -14;

        //TODO: refactor this into custom service.
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