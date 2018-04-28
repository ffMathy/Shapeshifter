namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    using Services.Clipboard.Interfaces;

    public class ClipboardFileData: IClipboardFileData
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }

        public byte[] FileIcon { get; set; }
        public byte[] RawData { get; set; }

        public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }
	}
}