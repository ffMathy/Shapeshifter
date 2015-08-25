using System.Collections.Generic;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
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

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }
    }
}
