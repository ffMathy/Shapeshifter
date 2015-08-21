using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces
{
    public interface IClipboardCombinationMediator : IHookService
    {
        event EventHandler<PasteCombinationHeldDownEventArgument> PasteCombinationHeldDown;
        event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        bool IsCombinationHeldDown { get; }
    }
}
