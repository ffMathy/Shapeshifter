using System;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardFileData : IClipboardFileData
    {
        private readonly IDataSource source;

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

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
