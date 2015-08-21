using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IPasteHotkeyInterceptor : IHookService
    {
        event EventHandler<PasteHotkeyFiredArgument> PasteHotkeyFired;

        void SendPasteCombination();
    }
}
