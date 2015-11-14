#region

using System.Collections.Generic;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardDataPackage
    {
        void AddData(IClipboardData data);

        IEnumerable<IClipboardData> Contents { get; }
    }
}