using System;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardImageData : IClipboardData
    {
        private readonly IDataSource source;

        public ClipboardImageData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
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
