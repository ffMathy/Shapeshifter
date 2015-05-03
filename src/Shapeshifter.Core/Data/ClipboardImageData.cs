using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardImageData : IClipboardData
    {
        private readonly IDataSource source;

        public ClipboardImageData(IDataSourceService sourceFactory)
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
