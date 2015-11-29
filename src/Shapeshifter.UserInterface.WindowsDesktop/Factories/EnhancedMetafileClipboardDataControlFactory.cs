namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    using System;

    using Controls.Clipboard.Interfaces;

    using Data.Interfaces;

    using Interfaces;

    class EnhancedMetafileClipboardDataControlFactory: IClipboardDataControlFactory
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
            return false;

            //TODO: && format == ClipboardApi.CF_ENHMETAFILE;
        }
    }
}