namespace Shapeshifter.WindowsDesktop.Data
{
	using Interfaces;

	using Services.Clipboard.Interfaces;

	public class ClipboardImageData : IClipboardImageData
	{
		public ClipboardImageData(IDataSourceService sourceFactory)
		{
			Source = sourceFactory.GetDataSource();
		}

		public byte[] Image { get; set; }

		public byte[] RawData { get; set; }

		public uint RawFormat { get; set; }

		public IDataSource Source { get; }
	}
}