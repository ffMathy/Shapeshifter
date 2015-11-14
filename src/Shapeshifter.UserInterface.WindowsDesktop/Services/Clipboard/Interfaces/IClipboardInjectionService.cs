#region

using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardInjectionService
    {
        void InjectData(IClipboardDataPackage package);
        void InjectImage(BitmapSource image);
        void InjectText(string text);
    }
}