using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardDataPackage
    {
        void AddData(IClipboardData data);

        IEnumerable<IClipboardData> Contents { get; }
    }
}
