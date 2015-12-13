namespace Shapeshifter.WindowsDesktop.Services.Messages.Interfaces
{
    using System;

    using Infrastructure.Dependencies.Interfaces;
    using Infrastructure.Events;

    public interface IWindowMessageInterceptor: ISingleInstance
    {
        void Install(IntPtr windowHandle);

        void Uninstall();

        void ReceiveMessageEvent(WindowMessageReceivedArgument e);
    }
}