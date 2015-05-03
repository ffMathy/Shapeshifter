namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IHookService
    {

        bool IsConnected { get; }

        void Disconnect();
        void Connect();
    }
}