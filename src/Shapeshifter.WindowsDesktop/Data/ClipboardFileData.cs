namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    using Services.Clipboard.Interfaces;

    public class ClipboardFileData: IClipboardFileData
    {
        public ClipboardFileData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public byte[] FileIcon { get; set; }

        public IDataSource Source { get; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }
    }
}