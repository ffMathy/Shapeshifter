using Shapeshifter.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.WindowsDesktop.Data
{
	class ClipboardCustomData : IClipboardCustomData
	{
		public byte[] RawData { get; set; }

		public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }
	}
}
