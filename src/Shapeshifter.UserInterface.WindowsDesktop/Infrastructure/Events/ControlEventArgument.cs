namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events
{
    using System;

    using Data.Interfaces;

    public class ControlEventArgument: EventArgs
    {
        public ControlEventArgument(IClipboardDataControlPackage package)
        {
            Package = package;
        }

        public IClipboardDataControlPackage Package { get; private set; }
    }
}