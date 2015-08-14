namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IProcessManager
    {
        void StartProcess(string fileName, string arguments = null);
    }
}
