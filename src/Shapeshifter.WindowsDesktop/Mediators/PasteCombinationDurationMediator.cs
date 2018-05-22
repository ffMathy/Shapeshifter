namespace Shapeshifter.WindowsDesktop.Mediators
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Controls.Window.ViewModels.Interfaces;

	using Infrastructure.Events;
	using Infrastructure.Threading.Interfaces;

	using Interfaces;
	using Serilog;
	using Services.Keyboard.Interfaces;

	class PasteCombinationDurationMediator: IPasteCombinationDurationMediator
    {
        readonly IPasteDetectionHandler pasteDetectionHandler;
        readonly IConsumerThreadLoop consumerLoop;
        readonly ISettingsViewModel settingsViewModel;
        readonly IThreadDelay threadDelay;
        readonly ILogger logger;
        readonly IKeyboardPasteCombinationStateService keyboardPasteState;
        readonly IMainThreadInvoker mainThreadInvoker;
        
        readonly CancellationTokenSource threadCancellationTokenSource;

        bool shouldCancel;

        public event EventHandler<PasteCombinationDurationPassedEventArgument> PasteCombinationDurationPassed;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;
        public event EventHandler<PasteCombinationReleasedEventArgument> AfterPasteCombinationReleased;
        public event EventHandler PasteCombinationHeldDown;

        public PasteCombinationDurationMediator(
            IPasteDetectionHandler pasteDetectionHandler,
            IConsumerThreadLoop consumerLoop,
            ISettingsViewModel settingsViewModel,
            IThreadDelay threadDelay,
            IMainThreadInvoker mainThreadInvoker,
            ILogger logger,
            IKeyboardPasteCombinationStateService keyboardPasteState)
        {
            this.pasteDetectionHandler = pasteDetectionHandler;
            this.consumerLoop = consumerLoop;
            this.settingsViewModel = settingsViewModel;
            this.threadDelay = threadDelay;
            this.mainThreadInvoker = mainThreadInvoker;
            this.logger = logger;
            this.keyboardPasteState = keyboardPasteState;

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
            => settingsViewModel.PasteDurationBeforeUserInterfaceShowsInMilliseconds / 100;

        public void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already connected.");

            pasteDetectionHandler.Connect();
            InstallPasteHotkeyInterceptor();
        }

        async Task MonitorClipboardCombinationStateAsync()
        {
            logger.Verbose("Paste combination duration loop has ticked.");

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
                keyboardPasteState.IsCombinationPartiallyHeldDown && 
                !shouldCancel)
            {
                await threadDelay.ExecuteAsync(100);
                decisecondsPassed++;
				
				if(decisecondsPassed <= DurationInDeciseconds)
					logger.Information($"Paste combination held down for {decisecondsPassed} deciseconds.");

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
			}
		}

        void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if ((DurationInDeciseconds != 0) && ((decisecondsPassed != DurationInDeciseconds) || (PasteCombinationDurationPassed == null)))
                return;

			logger.Information("Raising paste duration passed event.");
			mainThreadInvoker.Invoke(
                () => {
                    PasteCombinationDurationPassed?.Invoke(
                        this,
                        new PasteCombinationDurationPassedEventArgument
                            ());
                });
        }

        void InstallPasteHotkeyInterceptor()
        {
            pasteDetectionHandler.PasteDetected += PasteHotkeyInterceptor_PasteDetected;
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteDetectionHandler.PasteDetected -= PasteHotkeyInterceptor_PasteDetected;
        }

        void PasteHotkeyInterceptor_PasteDetected(object sender, EventArgs e)
        {
            logger.Information(
                "Paste combination duration mediator reacted to paste hotkey.");

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

            pasteDetectionHandler.Disconnect();
            threadCancellationTokenSource.Cancel();
            UninstallPasteHotkeyInterceptor();
        }

        protected virtual void OnPasteCombinationHeldDown()
        {
            PasteCombinationHeldDown?.Invoke(this, EventArgs.Empty);
        }
    }
}