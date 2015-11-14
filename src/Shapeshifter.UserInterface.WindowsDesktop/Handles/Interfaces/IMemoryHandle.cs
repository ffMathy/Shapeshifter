using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces
{
    public interface IMemoryHandle : IHandle
    {
        IntPtr Pointer { get; }
    }
}