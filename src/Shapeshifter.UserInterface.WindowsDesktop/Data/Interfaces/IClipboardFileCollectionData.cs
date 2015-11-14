#region

using System.Collections.Generic;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardFileCollectionData : IClipboardData
    {
        IEnumerable<IClipboardFileData> Files { get; set; }
    }
}