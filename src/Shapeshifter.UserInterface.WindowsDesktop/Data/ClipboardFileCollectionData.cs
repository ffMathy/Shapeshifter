namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    using System.Collections.Generic;

    using Interfaces;

    using Services.Interfaces;

    public class ClipboardFileCollectionData: IClipboardFileCollectionData
    {
        public ClipboardFileCollectionData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public IReadOnlyCollection<IClipboardFileData> Files { get; set; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }

        public IDataSource Source { get; }
    }
}