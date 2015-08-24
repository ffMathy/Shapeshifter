using System;
using System.Windows.Media.Imaging;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardInjectionService : IClipboardInjectionService
    {
        private readonly IClipboardCopyInterceptor clipboardCopyInterceptor;

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
            throw new NotImplementedException();
        }

        public void InjectText(string text)
        {
            clipboardCopyInterceptor.SkipNext();
            throw new NotImplementedException();
        }
    }
}
