using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Api
{
    [ExcludeFromCodeCoverage]
    class WindowApi
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        public static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);
        
        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return new IntPtr(GetClassLong32(hWnd, nIndex));
            else
                return GetClassLong64(hWnd, nIndex);
        }
        
        public static uint WM_GETICON = 0x007f;
        public static IntPtr ICON_BIG = new IntPtr(1);
        public static IntPtr IDI_APPLICATION = new IntPtr(0x7F00);
        public static int GCL_HICON = -14;
    }
}
