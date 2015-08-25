using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardInjectionService
    {
        void InjectImage(BitmapSource image);

        void InjectData(byte[] data);

        void InjectText(string text);
    }
}
