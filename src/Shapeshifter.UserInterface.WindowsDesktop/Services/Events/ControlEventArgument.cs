using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    [ExcludeFromCodeCoverage]
    class ControlEventArgument
    {
        public ControlEventArgument(ClipboardDataControlPackage package)
        {
            Package = package;
        }

        public ClipboardDataControlPackage Package { get; private set; }
    }
}
