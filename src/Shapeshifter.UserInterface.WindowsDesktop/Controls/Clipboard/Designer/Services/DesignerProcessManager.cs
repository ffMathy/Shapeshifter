namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using WindowsDesktop.Services.Interfaces;

    using Interfaces;

    
    class DesignerProcessManager
        : IProcessManager,
          IDesignerService
    {
        public void LaunchCommand(string command, string arguments = null) { }

        public void CloseAllProcessesExceptCurrent()
        {
            
        }

        public void LaunchFile(string fileName, string arguments = null) { }

        public void Dispose()
        {
            
        }
    }
}