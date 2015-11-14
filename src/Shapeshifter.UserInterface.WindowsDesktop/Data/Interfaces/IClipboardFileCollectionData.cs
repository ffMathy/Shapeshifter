using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardFileCollectionData : IClipboardData
    {
        IReadOnlyCollection<IClipboardFileData> Files { get; set; }
    }
}