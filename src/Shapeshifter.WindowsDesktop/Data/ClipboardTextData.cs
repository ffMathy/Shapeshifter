namespace Shapeshifter.WindowsDesktop.Data
{
	using Interfaces;

	public class ClipboardTextData : IClipboardTextData
	{
		public string Text { get; set; }

		public byte[] RawData { get; set; }

		public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }
	}
}