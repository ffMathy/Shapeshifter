namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces
{
    using System;

    public interface IMemoryHandle: IHandle
    {
        IntPtr Pointer { get; }
    }
}