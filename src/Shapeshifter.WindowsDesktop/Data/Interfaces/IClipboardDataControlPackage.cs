namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    using Controls.Clipboard.Interfaces;

    public interface IClipboardDataControlPackage
    {
        IClipboardDataPackage Data { get; }
        IClipboardControl Control { get; }

		IClipboardDataControlPackage Clone();
    }
}