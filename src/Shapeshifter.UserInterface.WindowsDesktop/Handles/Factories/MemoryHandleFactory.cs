using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories
{
    internal class MemoryHandleFactory : IMemoryHandleFactory
    {
        public IMemoryHandle AllocateInMemory(byte[] data)
        {
            return new MemoryHandle(data);
        }
    }
}