using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces
{
    public interface IWindowMessageInterceptor
    {
        void Install(IntPtr windowHandle);
        void Uninstall(IntPtr windowHandle);

        void ReceiveMessageEvent(WindowMessageReceivedArgument e);
    }
}
