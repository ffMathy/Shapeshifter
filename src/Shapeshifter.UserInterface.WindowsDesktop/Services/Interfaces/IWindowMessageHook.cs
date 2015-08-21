using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IWindowMessageHook : IHookService
    {
        event EventHandler<WindowMessageReceivedArgument> MessageReceived;

        IntPtr MainWindowHandle { get; }
    }
}
