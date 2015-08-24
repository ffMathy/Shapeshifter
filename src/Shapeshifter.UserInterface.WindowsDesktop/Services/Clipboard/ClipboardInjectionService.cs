using System;
using System.Windows.Media.Imaging;
using Shapeshifter.Core.Data;
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

        public void InjectData(IClipboardData clipboardData)
        {
            clipboardCopyInterceptor.SkipNext();
            throw new NotImplementedException();
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
