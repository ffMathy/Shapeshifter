namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    using System.Collections.Generic;

    public interface IClipboardDataPackage
    {
        void AddData(IClipboardData data);

        IReadOnlyList<IClipboardData> Contents { get; }
    }
}