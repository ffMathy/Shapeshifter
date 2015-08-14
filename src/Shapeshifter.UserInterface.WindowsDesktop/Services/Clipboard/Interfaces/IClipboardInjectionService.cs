using System.Windows.Media.Imaging;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardInjectionService
    {
        void InjectImage(BitmapSource image);

        void InjectData(IClipboardData data);

        void InjectText(string text);
    }
}
