using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    class ControlEventArgument
    {
        public ControlEventArgument(ClipboardDataControlPackage package)
        {
            Package = package;
        }

        public ClipboardDataControlPackage Package { get; private set; }
    }
}
