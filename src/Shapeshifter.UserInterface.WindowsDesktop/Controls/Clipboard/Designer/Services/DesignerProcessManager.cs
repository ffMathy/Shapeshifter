namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Diagnostics.CodeAnalysis;

    using WindowsDesktop.Services.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class DesignerProcessManager
        : IProcessManager,
          IDesignerService
    {
        public void LaunchCommand(string command) { }

        public void LaunchFile(string fileName, string arguments = null) { }
    }
}