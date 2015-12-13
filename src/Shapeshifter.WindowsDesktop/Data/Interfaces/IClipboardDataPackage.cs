namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    using System.Collections.Generic;

    public interface IClipboardDataPackage
    {
        void AddData(IClipboardData data);

        long Id { get; }

        IReadOnlyList<IClipboardData> Contents { get; }
    }
}