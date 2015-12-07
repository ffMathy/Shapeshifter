namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

    using Structures;

    interface IClipboardDataPackageFactory
    {
        IClipboardDataPackage CreateFromCurrentClipboardData();

        IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData);
    }
}
