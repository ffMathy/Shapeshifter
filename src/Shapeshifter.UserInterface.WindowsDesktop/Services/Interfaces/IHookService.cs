using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IHookService : ISingleInstance
    {

        bool IsConnected { get; }

        void Disconnect();
        void Connect();
    }
}