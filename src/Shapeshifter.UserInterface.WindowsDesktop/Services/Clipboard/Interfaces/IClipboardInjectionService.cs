using System.Windows.Media.Imaging;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardInjectionService
    {
        void InjectImage(BitmapSource image);

        void InjectData(IDataObject data);

        void InjectText(string text);
    }
}
