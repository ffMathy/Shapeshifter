namespace Shapeshifter.WindowsDesktop.Services.Messages.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Shared.Infrastructure.Dependencies.Interfaces;

    public interface IWindowMessageInterceptor: ISingleInstance
    {
        void Install(IntPtr windowHandle);

        void Uninstall();

        void ReceiveMessageEvent(WindowMessageReceivedArgument e);
    }
}