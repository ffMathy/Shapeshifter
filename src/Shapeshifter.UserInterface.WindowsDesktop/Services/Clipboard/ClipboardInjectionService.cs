using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardInjectionService : IClipboardInjectionService
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;

        public ClipboardInjectionService(
            IClipboardCopyInterceptor clipboardCopyInterceptor)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
        }

        public void InjectData(
            byte[] rawClipboardData)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetDataObject(rawClipboardData, true);
        }

        public void InjectImage(BitmapSource image)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetImage(image);
        }

        public void InjectText(string text)
        {
            clipboardCopyInterceptor.SkipNext();
            WindowsClipboard.SetText(text);
        }
    }
}
