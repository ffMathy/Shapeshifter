using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces
{
    public interface IPasteCombinationDurationMediator : IHookService
    {
        event EventHandler<PasteCombinationDurationPassedEventArgument> PasteCombinationDurationPassed;
        event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        int DurationInDeciseconds { get; }

        bool IsCombinationHeldDown { get; }
    }
}
