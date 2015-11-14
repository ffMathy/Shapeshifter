#region

using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IHookService : ISingleInstance
    {
        bool IsConnected { get; }

        void Disconnect();
        void Connect(IWindow window);
    }
}