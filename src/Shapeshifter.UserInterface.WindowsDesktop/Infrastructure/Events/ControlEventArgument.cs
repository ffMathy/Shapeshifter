#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events
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