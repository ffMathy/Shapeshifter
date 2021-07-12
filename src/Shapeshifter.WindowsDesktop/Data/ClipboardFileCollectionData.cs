namespace Shapeshifter.WindowsDesktop.Data
{
    using System.Collections.Generic;

    using Interfaces;

	public class ClipboardFileCollectionData: IClipboardFileCollectionData
    {
        public IReadOnlyCollection<IClipboardFileData> Files { get; set; }

        public byte[] RawData { get; set; }

        public IClipboardFormat RawFormat { get; set; }
		public IClipboardDataPackage Package { get; set; }

		public string ContentHash
		{
			get
			{
				var hash = string.Empty;
				foreach (var file in Files)
				{
					hash += file.FullPath + "/" + file.FileIcon.Length;
				}

				return hash;
			}
		}
	}
}