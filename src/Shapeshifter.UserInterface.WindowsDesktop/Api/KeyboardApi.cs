using System;
using System.Runtime.InteropServices;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Api
{
    static class KeyboardApi
    {
        public const int MOD_ALT = 0x1;
        public const int MOD_CONTROL = 0x2;
        public const int MOD_SHIFT = 0x4;
        public const int MOD_WIN = 0x8;
        public const int MOD_NOREPEAT = 0x4000;

        public const int WM_HOTKEY = 0x312;

        public const int VK_KEY_V = 0x56;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
