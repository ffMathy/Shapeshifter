namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    using System;

    public interface IProcessManager: IDisposable
    {
        void LaunchFile(string fileName, string arguments = null);

        void LaunchCommand(string command, string arguments = null);

        void CloseAllProcessesExceptCurrent();
    }
}