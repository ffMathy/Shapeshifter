namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Keyboard.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    class PasteCombinationDurationMediator: IPasteCombinationDurationMediator
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IConsumerThreadLoop consumerLoop;
        readonly IThreadDelay threadDelay;
        readonly ILogger logger;
        readonly IPasteCombinationStateService pasteState;
        readonly IMainThreadInvoker mainThreadInvoker;
        
        readonly CancellationTokenSource threadCancellationTokenSource;

        bool shouldCancel;

        public event EventHandler<PasteCombinationDurationPassedEventArgument> PasteCombinationDurationPassed;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;
        public event EventHandler<PasteCombinationReleasedEventArgument> AfterPasteCombinationReleased;
        public event EventHandler PasteCombinationHeldDown;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IConsumerThreadLoop consumerLoop,
            IThreadDelay threadDelay,
            IMainThreadInvoker mainThreadInvoker,
            ILogger logger,
            IPasteCombinationStateService pasteState)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.consumerLoop = consumerLoop;
            this.threadDelay = threadDelay;
            this.mainThreadInvoker = mainThreadInvoker;
            this.logger = logger;
            this.pasteState = pasteState;

            threadCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected
            => consumerLoop.IsRunning;

        bool IsCancellationRequested
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        public void CancelCombinationRegistration()
        {
            logger.Information("Cancelling duration mediator combination registration.");
            shouldCancel = true;
        }

        public int DurationInDeciseconds
            => 5;

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already connected.");
            }

            InstallPasteHotkeyInterceptor();
        }

        async Task MonitorClipboardCombinationStateAsync()
        {
            logger.Information("Paste combination duration loop has ticked.");

            shouldCancel = false;

            await WaitForCombinationReleaseOrDurationPass();
            if (IsCancellationRequested)
            {
                return;
            }

            RegisterCombinationReleased();
            if (shouldCancel)
            {
                return;
            }

            RegisterAfterCombinationReleased();
        }

        void RegisterCombinationReleased()
        {
            if (PasteCombinationReleased == null)
            {
                return;
            }

            logger.Information("Firing " + nameof(PasteCombinationReleased) + " event.");
            mainThreadInvoker.Invoke(
                () =>
                PasteCombinationReleased(
                    this,
                    new PasteCombinationReleasedEventArgument
                        ()));
        }

        void RegisterAfterCombinationReleased()
        {
            if (AfterPasteCombinationReleased == null)
            {
                return;
            }

            logger.Information("Firing " + nameof(AfterPasteCombinationReleased) + " event.");
            mainThreadInvoker.Invoke(
                () =>
                AfterPasteCombinationReleased(
                    this,
                    new PasteCombinationReleasedEventArgument
                        ()));
        }

        async Task WaitForCombinationReleaseOrDurationPass()
        {
            var decisecondsPassed = 0;
            while (
                !IsCancellationRequested &&
                pasteState.IsCombinationFullyHeldDown && 
                !shouldCancel)
            {
                await threadDelay.ExecuteAsync(100);
                decisecondsPassed++;

                logger.Information($"Paste combination held down for {decisecondsPassed}.");

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
            }
        }

        void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if ((decisecondsPassed != DurationInDeciseconds) || (PasteCombinationDurationPassed == null))
            {
                return;
            }

            mainThreadInvoker.Invoke(
                () => {
                    PasteCombinationDurationPassed(
                        this,
                        new PasteCombinationDurationPassedEventArgument
                            ());
                });
            logger.Information("Paste duration passed event raised.");
        }

        void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired -= PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, HotkeyFiredArgument e)
        {
            logger.Information(
                "Paste combination duration mediator reacted to paste hotkey.", 1);

            OnPasteCombinationHeldDown();

            consumerLoop.Notify(
                MonitorClipboardCombinationStateAsync,
                threadCancellationTokenSource.Token);
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already disconnected.");
            }

            threadCancellationTokenSource.Cancel();
            UninstallPasteHotkeyInterceptor();
        }

        protected virtual void OnPasteCombinationHeldDown()
        {
            PasteCombinationHeldDown?.Invoke(this, EventArgs.Empty);
        }
    }
}