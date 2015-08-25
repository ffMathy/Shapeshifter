using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class EnhancedMetafileClipboardDataControlFactory : IClipboardDataControlFactory
    {
        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }

        public IClipboardData BuildData(uint format, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return false;
        }

        public bool CanBuildData(uint format)
        {
            return 
                format == ClipboardApi.CF_ENHMETAFILE;
        }
    }
}
