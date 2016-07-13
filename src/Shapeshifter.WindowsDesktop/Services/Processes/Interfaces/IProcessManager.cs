namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;

    public interface IProcessManager: IDisposable
    {
        void LaunchFile(string fileName, string arguments = null);

        void LaunchFileWithAdministrativeRights(string fileName, string arguments = null);

        bool IsCurrentProcessElevated();

        string GetCurrentProcessPath();

        void LaunchCommand(string command, string arguments = null);

        void CloseAllDuplicateProcessesExceptCurrent();
    }
}