namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using Infrastructure.Dependencies.Interfaces;

    public interface IHookService: ISingleInstance
    {
        bool IsConnected { get; }

        void Disconnect();

        void Connect();
    }
}