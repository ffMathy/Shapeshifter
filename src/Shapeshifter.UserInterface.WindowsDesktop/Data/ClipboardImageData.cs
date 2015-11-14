#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardImageData : IClipboardImageData
    {
        public ClipboardImageData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public byte[] Image
        {
            get { return RawData; }
        }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }

        public IDataSource Source { get; }
    }
}