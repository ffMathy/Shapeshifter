namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using Shared.Controls.Designer.Services;
    using Shared.Services.Interfaces;

    class DesignerProcessManager
        : IProcessManager,
          IDesignerService
    {
        public void LaunchCommand(string command, string arguments = null) { }

        public void CloseAllProcessesExceptCurrent() { }

        public void LaunchFile(string fileName, string arguments = null) { }

        public void Dispose() { }
    }
}