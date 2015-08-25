using System;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using System.Runtime.InteropServices;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles
{
    class MemoryHandle : IMemoryHandle
    {
        readonly GCHandle handle;

        public MemoryHandle(byte[] bytes)
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        }

        public IntPtr Pointer
        {
            get
            {
                return handle.AddrOfPinnedObject();
            }
        }

        public void Dispose()
        {
            handle.Free();
        }
    }
}
