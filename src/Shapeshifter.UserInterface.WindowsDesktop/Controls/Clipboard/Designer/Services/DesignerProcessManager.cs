using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DesignerProcessManager : IProcessManager, IDesignerService
    {
        public void LaunchCommand(string command)
        {
        }

        public void LaunchFile(string fileName, string arguments = null)
        {
        }
    }
}