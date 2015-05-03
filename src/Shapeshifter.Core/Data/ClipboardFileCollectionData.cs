using System;
using System.Collections.Generic;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardFileCollectionData : IClipboardData
    {
        private readonly IDataSource source;

        public ClipboardFileCollectionData(IDataSourceFactory sourceFactory)
        {
            this.source = sourceFactory.GetDataSource();
        }

        public IEnumerable<ClipboardFileData> Files { get; protected set; }

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
