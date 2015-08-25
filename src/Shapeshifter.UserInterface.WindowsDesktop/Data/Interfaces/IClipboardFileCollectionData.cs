using System.Collections.Generic;

namespace Shapeshifter.Core.Data.Interfaces
{
    public interface IClipboardFileCollectionData : IClipboardData
    {
        IEnumerable<IClipboardFileData> Files { get; set; }
    }
}
