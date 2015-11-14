#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    internal class EnhancedMetafileClipboardDataControlFactory : IClipboardDataControlFactory
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
            return false &&
                   format == ClipboardApi.CF_ENHMETAFILE;
        }
    }
}