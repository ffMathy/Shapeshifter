namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    using Controls.Clipboard.Interfaces;

    public interface IClipboardDataControlPackage
    {
        IClipboardDataPackage Data { get; }

        IClipboardControl Control { get; }
    }
}