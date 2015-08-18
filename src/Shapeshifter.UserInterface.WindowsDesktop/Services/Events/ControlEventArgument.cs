using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

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
