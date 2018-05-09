namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
	using System;

	using Data.Interfaces;

	public interface IClipboardDataPackageFactory
    {
        IClipboardDataPackage CreateFromCurrentClipboardData();

        IClipboardDataPackage CreateFromFormatsAndData(Guid packageId, params FormatDataPair[] formatsAndData);
    }
}