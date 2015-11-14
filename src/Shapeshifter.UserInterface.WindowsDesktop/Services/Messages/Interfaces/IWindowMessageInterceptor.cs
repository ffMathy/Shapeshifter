using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces
{
    public interface IWindowMessageInterceptor : ISingleInstance
    {
        void Install(IntPtr windowHandle);

        void Uninstall();

        void ReceiveMessageEvent(WindowMessageReceivedArgument e);
    }
}
