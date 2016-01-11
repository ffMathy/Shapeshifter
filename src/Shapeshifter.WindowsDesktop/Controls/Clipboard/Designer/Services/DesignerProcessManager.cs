namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using WindowsDesktop.Services.Interfaces;

    using Controls.Designer.Services;

    class DesignerProcessManager
        : IProcessManager,
          IDesignerService
    {
        public string GetCurrentProcessPath()
        {
            throw new System.NotImplementedException();
        }

        public void LaunchCommand(string command, string arguments = null) { }

        public void CloseAllProcessesExceptCurrent() { }

        public void LaunchFile(string fileName, string arguments = null) { }

        public void LaunchFileWithAdministrativeRights(string fileName, string arguments = null)
        {
        }

        public bool IsCurrentProcessElevated()
        {
            return false;
        }

        public void Dispose() { }
    }
}