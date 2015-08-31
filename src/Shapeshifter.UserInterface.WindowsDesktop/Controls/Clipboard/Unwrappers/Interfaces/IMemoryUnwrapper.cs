using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces
{
    public interface IMemoryUnwrapper
    {
        bool CanUnwrap(uint format);

        byte[] UnwrapStructure(uint format);
    }
}
