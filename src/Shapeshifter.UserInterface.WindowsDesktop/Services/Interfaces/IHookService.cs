namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    using Windows.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    public interface IHookService: ISingleInstance
    {
        bool IsConnected { get; }

        void Disconnect();

        void Connect(IWindow window);
    }
}