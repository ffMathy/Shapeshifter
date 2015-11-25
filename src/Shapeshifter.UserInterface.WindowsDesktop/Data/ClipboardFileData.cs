using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardFileData : IClipboardFileData
    {
        public ClipboardFileData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public string FileName { get; set; }
        public string FullPath { get; set; }
        public byte[] FileIcon { get; set; }

        public IDataSource Source { get; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }
    }
}