using System;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardFileData : IClipboardData
    {
        private readonly IDataSource source;

        public ClipboardFileData(IDataSourceFactory sourceFactory)
        {
            this.source = sourceFactory.GetDataSource();
        }

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
