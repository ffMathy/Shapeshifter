using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardTextData : IClipboardTextData
    {
        readonly IDataSource source;

        public ClipboardTextData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
        }

        public string Text { get; set; }

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

        public uint RawFormat
        {
            get; set;
        }
    }
}
