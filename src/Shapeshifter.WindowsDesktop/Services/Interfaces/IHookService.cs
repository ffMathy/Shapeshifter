namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Shared.Controls.Window.Interfaces;

    public interface IHookService: ISingleInstance
    {
        bool IsConnected { get; }

        void Disconnect();

        void Connect(IHookableWindow window);
    }
}