using Shapeshifter.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data.Unwrappers.Interfaces
{
    public interface IMemoryUnwrapper
    {
        bool CanUnwrap(IClipboardFormat format);

        byte[] UnwrapStructure(IClipboardFormat format);
    }
}