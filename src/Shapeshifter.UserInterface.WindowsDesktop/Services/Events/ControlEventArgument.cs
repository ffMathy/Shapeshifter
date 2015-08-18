using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    public class ControlEventArgument
    {
        public ControlEventArgument(IClipboardControlDataPackage package)
        {
            Package = package;
        }

        public IClipboardControlDataPackage Package { get; private set; }
    }
}
