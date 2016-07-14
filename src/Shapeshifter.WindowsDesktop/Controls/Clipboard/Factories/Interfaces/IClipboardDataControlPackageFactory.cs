namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataControlPackageFactory
    {
        IClipboardDataControlPackage CreateFromCurrentClipboardData();
        IClipboardDataControlPackage CreateFromDataPackage(IClipboardDataPackage package);
    }
}