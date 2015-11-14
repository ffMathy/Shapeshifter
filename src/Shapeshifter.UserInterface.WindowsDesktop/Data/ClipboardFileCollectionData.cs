using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardFileCollectionData : IClipboardFileCollectionData
    {
        readonly IDataSource source;

        public ClipboardFileCollectionData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
        }

        public IEnumerable<IClipboardFileData> Files { get; set; }

        public byte[] RawData
        {
            get; set;
        }

        public uint RawFormat { get; set; }

        public IDataSource Source
            => source;
    }
}
