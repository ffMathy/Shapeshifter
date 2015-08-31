using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardImageData : IClipboardImageData
    {
        readonly IDataSource source;

        public ClipboardImageData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
        }

        public byte[] Image
        {
            get; set;
        }

        public byte[] RawData
        {
            get; set;
        }

        public uint RawFormat { get; set; }

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }
    }
}
