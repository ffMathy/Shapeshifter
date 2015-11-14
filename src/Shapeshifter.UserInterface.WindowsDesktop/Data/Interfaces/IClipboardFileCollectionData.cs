using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardFileCollectionData : IClipboardData
    {
        IEnumerable<IClipboardFileData> Files { get; set; }
    }
}
