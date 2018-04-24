namespace Shapeshifter.WindowsDesktop.Services.Messages.Interfaces
{
    using System;
	using System.Threading.Tasks;
	using Infrastructure.Dependencies.Interfaces;
    using Infrastructure.Events;

    public interface IWindowMessageInterceptor: ISingleInstance
    {
        void Install(IntPtr windowHandle);

        void Uninstall();

        Task ReceiveMessageEventAsync(WindowMessageReceivedArgument e);
    }
}