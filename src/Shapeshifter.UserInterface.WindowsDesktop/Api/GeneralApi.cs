using System;
using System.Runtime.InteropServices;

namespace Shapeshifter.UserInterface.WindowsDesktop.Api
{
    public static class GeneralApi
    {
        public const int GMEM_MOVABLE = 0x0002;
        public const int GMEM_ZEROINIT = 0x0040;

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = true)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);
    }
}
