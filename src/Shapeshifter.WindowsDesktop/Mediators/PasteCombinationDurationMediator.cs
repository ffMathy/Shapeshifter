namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Keyboard.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    class PasteCombinationDurationMediator : IPasteCombinationDurationMediator
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IConsumerThreadLoop consumerLoop;
        readonly IThreadDelay threadDelay;
        readonly ILogger logger;
        readonly IKeyboardManager keyboardManager;
        readonly IMainThreadInvoker mainThreadInvoker;

        readonly CancellationTokenSource threadCancellationTokenSource;

        bool combinationCancellationRequested;

        public event EventHandler<PasteCombinationDurationPassedEventArgument>
            PasteCombinationDurationPassed;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleasedPartially;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleasedEntirely;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IConsumerThreadLoop consumerLoop,
            IThreadDelay threadDelay,
            IMainThreadInvoker mainThreadInvoker,
            ILogger logger,
            IKeyboardManager keyboardManager)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.consumerLoop = consumerLoop;
            this.threadDelay = threadDelay;
            this.mainThreadInvoker = mainThreadInvoker;
            this.logger = logger;
            this.keyboardManager = keyboardManager;

            threadCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected
            => consumerLoop.IsRunning;

        bool IsCancellationRequested
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        public bool IsCombinationFullyHeldDown
            => keyboardManager.IsKeyDown(Key.LeftCtrl) && keyboardManager.IsKeyDown(Key.V);

        public void CancelCombinationRegistration()
        {
            combinationCancellationRequested = true;
        }

        public bool IsCombinationPartiallyHeldDown
            => keyboardManager.IsKeyDown(Key.LeftCtrl) || keyboardManager.IsKeyDown(Key.V);

        public int DurationInDeciseconds
            => 5;

        public void Connect(IWindow targetWindow)
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
            combinationCancellationRequested = false;

            await WaitForCombinationReleasePartially();
            if (IsCancellationRequested)
            {
                return;
            }

            RegisterCombinationReleasedPartially();
            if (combinationCancellationRequested)
            {
                return;
            }

            await WaitForCombinationReleaseEntirely();
            if (IsCancellationRequested)
            {
                return;
            }

            RegisterCombinationReleasedEntirely();
        }

        void RegisterCombinationReleasedPartially()
        {
            if (PasteCombinationReleasedPartially != null)
            {
                mainThreadInvoker.Invoke(
                    () =>
                    PasteCombinationReleasedPartially(
                        this,
                        new PasteCombinationReleasedEventArgument
                            ()));
            }
        }

        void RegisterCombinationReleasedEntirely()
        {
            if (PasteCombinationReleasedEntirely != null)
            {
                mainThreadInvoker.Invoke(
                    () =>
                    PasteCombinationReleasedEntirely(
                        this,
                        new PasteCombinationReleasedEventArgument
                            ()));
            }
        }

        async Task WaitForCombinationReleaseEntirely()
        {
            while (!IsCancellationRequested && IsCombinationPartiallyHeldDown)
            {
                await threadDelay.ExecuteAsync(100);
            }
        }

        async Task WaitForCombinationReleasePartially()
        {
            var decisecondsPassed = 0;
            while (!IsCancellationRequested && IsCombinationFullyHeldDown && !combinationCancellationRequested)
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
                () =>
                PasteCombinationDurationPassed(
                    this,
                    new PasteCombinationDurationPassedEventArgument
                        ()));
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
                "Paste combination duration mediator reacted to paste hotkey.",
                1);

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
    }
}