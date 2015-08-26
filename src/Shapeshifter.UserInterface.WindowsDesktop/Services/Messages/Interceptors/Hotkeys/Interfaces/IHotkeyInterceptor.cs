using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IHotkeyInterceptor : IWindowMessageInterceptor
    {
        event EventHandler<HotkeyFiredArgument> HotkeyFired;
    }
}
