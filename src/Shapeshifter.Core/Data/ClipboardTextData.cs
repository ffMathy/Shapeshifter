using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardTextData : IClipboardData
    {
        private readonly IDataSource source;

        public ClipboardTextData(IDataSourceService sourceFactory)
        {
            this.source = sourceFactory.GetDataSource();
        }

        public string Text { get; set; }

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(Text);
        }
    }
}
