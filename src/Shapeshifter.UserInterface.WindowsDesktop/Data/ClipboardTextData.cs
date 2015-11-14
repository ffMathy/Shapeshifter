#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardTextData : IClipboardTextData
    {
        public ClipboardTextData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public string Text { get; set; }

        public IDataSource Source { get; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }
    }
}