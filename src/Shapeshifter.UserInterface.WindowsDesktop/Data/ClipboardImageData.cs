namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    using Interfaces;

    using Services.Interfaces;

    public class ClipboardImageData: IClipboardImageData
    {
        public ClipboardImageData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public byte[] Image => RawData;

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }

        public IDataSource Source { get; }
    }
}