namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;
    using System.Runtime.InteropServices;

    using Interfaces;

    class MemoryHandle: IMemoryHandle
    {
        public MemoryHandle(byte[] bytes)
        {
            Pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, Pointer, bytes.Length);
        }

        public IntPtr Pointer { get; }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Pointer);
        }
    }
}