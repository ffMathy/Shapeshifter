using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    interface IClipboardInjectionService
    {
        void InjectImage(BitmapSource image);
    }
}
