namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
	using System;
	using System.Threading.Tasks;

	using Data.Interfaces;

	public interface IClipboardDataPackageFactory
    {
        Task<IClipboardDataPackage> CreateFromCurrentClipboardDataAsync();

        Task<IClipboardDataPackage> CreateFromFormatsAndDataAsync(Guid packageId, params FormatDataPair[] formatsAndData);
    }
}