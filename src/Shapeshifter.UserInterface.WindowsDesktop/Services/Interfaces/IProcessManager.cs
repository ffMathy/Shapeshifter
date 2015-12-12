namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IProcessManager
    {
        void LaunchFile(string fileName, string arguments = null);

        void LaunchCommand(string command);

        void CloseAllProcessesExceptCurrent();
    }
}