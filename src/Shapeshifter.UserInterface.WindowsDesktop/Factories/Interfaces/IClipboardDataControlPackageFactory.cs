namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataControlPackageFactory
    {
        IClipboardDataControlPackage CreateFromCurrentClipboardData();
    }
}