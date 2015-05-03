using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class DeviceIndependentBitmapClipboardDataFactory : IClipboardDataFactory
    {
        public IClipboardData Build(string format, object data)
        {
            throw new NotImplementedException();
        }

        public bool CanBuild(string format)
        {
            return format == "CF_DIB" || format == "CF_DIBV5" || format == "CF_BITMAP";
        }
    }
}
