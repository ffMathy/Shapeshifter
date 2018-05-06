namespace Shapeshifter.WindowsDesktop.Data
{
	using Interfaces;

	public class ClipboardImageData : IClipboardImageData
	{
		public byte[] Image { get; set; }

		public byte[] RawData { get; set; }

		public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }
	}
}