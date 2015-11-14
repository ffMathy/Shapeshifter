using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
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
