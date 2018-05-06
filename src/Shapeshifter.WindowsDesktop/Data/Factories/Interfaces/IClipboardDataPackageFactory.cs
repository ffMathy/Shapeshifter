namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

	public interface IClipboardDataPackageFactory
    {
        IClipboardDataPackage CreateFromCurrentClipboardData();

        IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData);
    }
}