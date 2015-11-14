#region

using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardFileCollectionData : IClipboardFileCollectionData
    {
        public ClipboardFileCollectionData(IDataSourceService sourceFactory)
        {
            Source = sourceFactory.GetDataSource();
        }

        public IEnumerable<IClipboardFileData> Files { get; set; }

        public byte[] RawData { get; set; }

        public uint RawFormat { get; set; }

        public IDataSource Source { get; }
    }
}