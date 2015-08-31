using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
