namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    using System.Collections.Generic;

    public interface IClipboardFileCollectionData: IClipboardData
    {
        IReadOnlyCollection<IClipboardFileData> Files { get; set; }
    }
}