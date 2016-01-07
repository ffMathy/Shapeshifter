namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Interfaces
{
    using System;

    using Shared.Infrastructure.Handles.Interfaces;

    public interface IMemoryHandle: IHandle
    {
        IntPtr Pointer { get; }
    }
}