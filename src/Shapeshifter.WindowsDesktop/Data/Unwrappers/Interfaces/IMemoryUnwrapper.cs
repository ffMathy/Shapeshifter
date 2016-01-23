namespace Shapeshifter.WindowsDesktop.Data.Unwrappers.Interfaces
{
    public interface IMemoryUnwrapper
    {
        bool CanUnwrap(uint format);

        byte[] UnwrapStructure(uint format);
    }
}