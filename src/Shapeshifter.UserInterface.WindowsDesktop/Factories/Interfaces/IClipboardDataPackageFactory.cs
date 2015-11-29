namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    using Data.Interfaces;

    interface IClipboardDataPackageFactory
    {
        IClipboardDataPackage CreateFromCurrentClipboardData();
    }
}
