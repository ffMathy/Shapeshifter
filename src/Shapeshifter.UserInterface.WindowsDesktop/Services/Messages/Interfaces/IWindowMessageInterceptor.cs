using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces
{
    public interface IWindowMessageInterceptor : ISingleInstance
    {
        bool IsManagedAutomatically { get; }

        void Install(IntPtr windowHandle);
        void Uninstall(IntPtr windowHandle);

        void ReceiveMessageEvent(WindowMessageReceivedArgument e);
    }
}
