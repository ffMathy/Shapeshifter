namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Services.Interfaces;

    public interface IPasteCombinationDurationMediator: IHookService
    {
        event EventHandler<PasteCombinationDurationPassedEventArgument>
            PasteCombinationDurationPassed;

        event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        int DurationInDeciseconds { get; }

        bool IsCombinationHeldDown { get; }
    }
}