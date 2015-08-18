using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public delegate void KeyEventHandler(object sender, KeyEventArgument e, ref bool blockKeystroke);

    public interface IKeyboardHookService : IHookService
    {
        event EventHandler<HookRecoveredEventArgument> HookRecovered;

        event KeyEventHandler KeyUp;
        event KeyEventHandler KeyDown;
    }
}
