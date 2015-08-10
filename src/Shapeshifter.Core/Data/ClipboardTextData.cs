using System.Text;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Core.Data
{
    public class ClipboardTextData : IClipboardTextData
    {
        private readonly IDataSource source;

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

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(Text);
        }
    }
}
