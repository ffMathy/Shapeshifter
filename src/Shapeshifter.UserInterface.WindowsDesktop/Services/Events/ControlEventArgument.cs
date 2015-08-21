using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    public class ControlEventArgument : EventArgs
    {
        public ControlEventArgument(IClipboardDataControlPackage package)
        {
            Package = package;
        }

        public IClipboardDataControlPackage Package { get; private set; }
    }
}
