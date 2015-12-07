namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Handles.Interfaces;

    using Interfaces;

    class MemoryHandleFactory: IMemoryHandleFactory
    {
        public IMemoryHandle AllocateInMemory(byte[] data)
        {
            return new MemoryHandle(data);
        }
    }
}