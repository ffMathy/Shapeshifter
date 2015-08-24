using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IPasteHotkeyInterceptor : IWindowMessageInterceptor
    {
        event EventHandler<PasteHotkeyFiredArgument> PasteHotkeyFired;

        void Disable();

        void Enable();
    }
}
