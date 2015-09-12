using System;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using System.Runtime.InteropServices;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Api;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    class MemoryHandle : IMemoryHandle
    {
        readonly IntPtr pointer;

        public MemoryHandle(byte[] bytes)
        {
            pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
        }

        public IntPtr Pointer
        {
            get
            {
                return pointer;
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(pointer);
        }
    }
}
