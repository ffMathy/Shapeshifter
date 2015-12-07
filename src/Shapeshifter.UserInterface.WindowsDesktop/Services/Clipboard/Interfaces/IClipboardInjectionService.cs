namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Windows.Media.Imaging;

    using Data.Interfaces;

    public interface IClipboardInjectionService
    {
        void InjectData(IClipboardDataPackage package);

        void InjectImage(BitmapSource image);

        void InjectText(string text);

        void InjectFiles(params string[] files);
    }
}