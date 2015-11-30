namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataControlPackageFactory
    {
        IClipboardDataControlPackage CreateFromCurrentClipboardData();
    }
}