namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    using Stability.Interfaces;

    public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
    {
        readonly IThreadLoop threadLoop;
        readonly IThreadDelay threadDelay;
        readonly IKeyboardPasteCombinationStateService keyboardPasteState;
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        int ticks;
        bool wasNotifiedOfCombination;

        public event EventHandler KeyboardAccessOverruled;
        public event EventHandler KeyboardAccessRestored;

        public KeyboardDominanceWatcher(
            IThreadLoop threadLoop,
            IThreadDelay threadDelay,
            IKeyboardPasteCombinationStateService keyboardPasteState,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor)
        {
            this.threadLoop = threadLoop;
            this.threadDelay = threadDelay;
            this.keyboardPasteState = keyboardPasteState;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;

            wasNotifiedOfCombination = true;

            SetupPasteDurationMediator();
        }

        void SetupPasteDurationMediator()
        {
            pasteHotkeyInterceptor.PasteDetected += PasteHotkeyInterceptor_PasteDetected;
        }

        void PasteHotkeyInterceptor_PasteDetected(object sender, EventArgs e)
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
            if (keyboardPasteState.IsCombinationFullyHeldDown)
            {
                HandleCombinationHeldDown();
            }
            else
            {
                ticks = 0;
            }

            await threadDelay.ExecuteAsync(100);
        }

        void HandleCombinationHeldDown()
        {
            const int ticksNeeded = 2;
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