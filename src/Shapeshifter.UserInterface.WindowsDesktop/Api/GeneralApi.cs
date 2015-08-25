using System;
using System.Runtime.InteropServices;

namespace Shapeshifter.UserInterface.WindowsDesktop.Api
{
    public static class GeneralApi
    {

        [DllImport("kernel32.dll")]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);
    }
}
