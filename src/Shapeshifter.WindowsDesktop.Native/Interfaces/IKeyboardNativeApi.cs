namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;
    using System.Runtime.InteropServices;

    public interface IKeyboardNativeApi
    {
        bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        uint SendInput(uint nInputs, [In] [MarshalAs(UnmanagedType.LPArray)] KeyboardNativeApi.INPUT[] pInputs, int cbSize);

        bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}