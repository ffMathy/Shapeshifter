namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

    using Structures;

    public interface IClipboardDataPackageFactory
    {
        IClipboardDataPackage CreateFromCurrentClipboardData();

        IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData);
    }
}