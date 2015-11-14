using System;
using System.Runtime.InteropServices;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    internal class MemoryHandle : IMemoryHandle
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