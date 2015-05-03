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

        public string FileName { get; protected set; }
        public byte[] FileIcon { get; protected set; }

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
