namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Threading.Interfaces;

    using Mediators.Interfaces;

    using Stability.Interfaces;

    public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
    {
        readonly IThreadLoop threadLoop;
        readonly IThreadDelay threadDelay;
        readonly IPasteCombinationDurationMediator pasteDurationMediator;

        int ticks;
        bool wasNotifiedOfCombination;

        public event EventHandler KeyboardAccessOverruled;
        public event EventHandler KeyboardAccessRestored;

        public KeyboardDominanceWatcher(
            IThreadLoop threadLoop,
            IThreadDelay threadDelay,
            IPasteCombinationDurationMediator pasteDurationMediator)
        {
            this.threadLoop = threadLoop;
            this.threadDelay = threadDelay;
            this.pasteDurationMediator = pasteDurationMediator;

            wasNotifiedOfCombination = true;

            SetupPasteDurationMediator();
        }

        void SetupPasteDurationMediator()
        {
            pasteDurationMediator.PasteCombinationHeldDown += PasteDurationMediator_PasteCombinationHeldDown;
        }

        void PasteDurationMediator_PasteCombinationHeldDown(
            object sender, EventArgs e)
        {
            if (!wasNotifiedOfCombination)
            {
                OnKeyboardAccessRestored();
            }

            wasNotifiedOfCombination = true;
            ticks = 0;
        }

        public void Start()
        {
            threadLoop.StartAsync(
                RunDetection);
        }

        async Task RunDetection()
        {
            if (pasteDurationMediator.IsCombinationFullyHeldDown)
            {
                HandleCombinationHeldDown();
            }
            else
            {
                ticks = 0;
            }

            await threadDelay.ExecuteAsync(10);
        }

        void HandleCombinationHeldDown()
        {
            const int ticksNeeded = 10;
            ticks++;

            var isOverruled =
                (ticks == ticksNeeded) &&
                !wasNotifiedOfCombination;
            if (isOverruled)
            {
                OnKeyboardAccessOverruled();
            }
        }

        public void Stop()
        {
            threadLoop.Stop();
        }

        protected virtual void OnKeyboardAccessOverruled()
        {
            KeyboardAccessOverruled?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnKeyboardAccessRestored()
        {
            KeyboardAccessRestored?.Invoke(this, EventArgs.Empty);
        }
    }
}