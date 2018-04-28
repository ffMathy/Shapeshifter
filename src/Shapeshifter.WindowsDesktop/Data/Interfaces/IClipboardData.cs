namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardData
    { 
        byte[] RawData { get; }

		IClipboardFormat RawFormat { get; }
		IClipboardDataPackage Package { get; set; }
    }
}