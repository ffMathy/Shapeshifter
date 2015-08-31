using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;
using System;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    class BitmapUnwrapper : IMemoryUnwrapper
    {
        public bool CanUnwrap(uint format)
        {
            throw new NotImplementedException();
        }

        public byte[] UnwrapStructure(uint format)
        {
            throw new NotImplementedException();
        }
    }
}
