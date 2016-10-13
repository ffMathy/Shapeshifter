namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    using Data.Interfaces;

    public interface IClipboardInjectionService
    {
        void ClearClipboard();

        Task InjectDataAsync(IClipboardDataPackage package);

        Task InjectImageAsync(BitmapSource image);

        Task InjectTextAsync(string text);

        Task InjectFilesAsync(params string[] files);
    }
}