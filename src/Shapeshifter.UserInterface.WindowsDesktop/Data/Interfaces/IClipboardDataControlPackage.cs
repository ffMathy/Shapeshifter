namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    using Controls.Clipboard.Interfaces;

    public interface IClipboardDataControlPackage: IClipboardDataPackage
    {
        IClipboardControl Control { get; }
    }
}