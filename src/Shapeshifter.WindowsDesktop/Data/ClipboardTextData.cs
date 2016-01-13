namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    using Services.Clipboard.Interfaces;

    public class ClipboardTextData: IClipboardTextData
    {
        public ClipboardTextData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public string Text { get; set; }

        public IDataSource Source { get; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }
    }
}