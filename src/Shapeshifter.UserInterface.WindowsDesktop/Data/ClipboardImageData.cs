using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardImageData : IClipboardImageData
    {
        readonly IDataSource source;

        public ClipboardImageData(IDataSourceService sourceFactory)
        {
            source = sourceFactory.GetDataSource();
        }

        public byte[] Image
        {
            get
            {
                return RawData;
            }
        }

        public byte[] RawData
        {
            get; set;
        }

        public uint RawFormat { get; set; }

        public IDataSource Source
        {
            get
            {
                return source;
            }
        }
    }
}
