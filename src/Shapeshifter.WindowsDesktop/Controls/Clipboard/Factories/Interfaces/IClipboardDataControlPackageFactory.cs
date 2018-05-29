namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
	using System.Threading.Tasks;

	using Data.Interfaces;

    public interface IClipboardDataControlPackageFactory
    {
        Task<IClipboardDataControlPackage> CreateFromCurrentClipboardDataAsync();
        IClipboardDataControlPackage CreateFromDataPackage(IClipboardDataPackage package);
    }
}