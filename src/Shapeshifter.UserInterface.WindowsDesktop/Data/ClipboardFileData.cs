using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardFileData : IClipboardFileData
    {
        readonly IDataSource source;

        public ClipboardFileData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
        }

        public string FileName { get; set; }
        public byte[] FileIcon { get; set; }

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }

        public byte[] RawData
        {
            get; set;
        }

        public uint RawFormat { get; set; }
    }
}
