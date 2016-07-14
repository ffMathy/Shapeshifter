namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;
    using System.Runtime.InteropServices;

    public interface IGeneralNativeApi
    {
        T ByteArrayToStructure<T>(byte[] data);

        void CopyMemory(IntPtr dest, IntPtr src, uint count);

        IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        IntPtr GlobalFree(IntPtr hMem);

        IntPtr GlobalLock(IntPtr hMem);

        UIntPtr GlobalSize(IntPtr hMem);

        bool GlobalUnlock(IntPtr hMem);

        byte[] StructureToByteArray<T>(T structure);

        void DwmGetColorizationColor(out uint ColorizationColor, [MarshalAs(UnmanagedType.Bool)] out bool ColorizationOpaqueBlend);
    }
}