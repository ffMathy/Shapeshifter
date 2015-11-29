namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    using Handles.Interfaces;

    public interface IMemoryHandleFactory
    {
        IMemoryHandle AllocateInMemory(byte[] data);
    }
}