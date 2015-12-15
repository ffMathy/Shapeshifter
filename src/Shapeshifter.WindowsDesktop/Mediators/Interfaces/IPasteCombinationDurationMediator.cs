namespace Shapeshifter.WindowsDesktop.Mediators.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Services.Interfaces;

    public interface IPasteCombinationDurationMediator: IHookService
    {
        event EventHandler<PasteCombinationDurationPassedEventArgument>
            PasteCombinationDurationPassed;

        event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleasedPartially;

        event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleasedEntirely;

        int DurationInDeciseconds { get; }

        bool IsCombinationHeldDown { get; }

        void CancelCombinationRegistration();
    }
}