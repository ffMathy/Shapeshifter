using System;
using System.Windows.Media.Imaging;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardInjectionService : IClipboardInjectionService
    {
        public void InjectData(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }

        public void InjectImage(BitmapSource image)
        {
            throw new NotImplementedException();
        }

        public void InjectText(string text)
        {
            throw new NotImplementedException();
        }
    }
}
