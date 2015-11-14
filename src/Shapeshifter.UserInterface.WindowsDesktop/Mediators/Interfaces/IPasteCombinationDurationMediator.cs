#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

#endregion

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