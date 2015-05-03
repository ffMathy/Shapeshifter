using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    delegate void KeyEventHandler(object sender, KeyEventArgument e, ref bool blockKeystroke);

    interface IKeyboardHookService : IHookService
    {
        event EventHandler<HookRecoveredEventArgument> HookRecovered;

        event KeyEventHandler KeyUp;
        event KeyEventHandler KeyDown;
    }
}
