namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Windows.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    class PasteCombinationDurationMediator: IPasteCombinationDurationMediator
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        readonly IConsumerThreadLoop threadLoop;

        readonly IThreadDelay threadDelay;

        readonly ILogger logger;

        readonly IMainThreadInvoker mainThreadInvoker;

        readonly CancellationTokenSource threadCancellationTokenSource;

        bool isCombinationDown;

        public event EventHandler<PasteCombinationDurationPassedEventArgument>
            PasteCombinationDurationPassed;

        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IConsumerThreadLoop threadLoop,
            IThreadDelay threadDelay,
            IMainThreadInvoker mainThreadInvoker,
            ILogger logger)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.threadLoop = threadLoop;
            this.threadDelay = threadDelay;
            this.mainThreadInvoker = mainThreadInvoker;
            this.logger = logger;

            threadCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected
            => threadLoop.IsRunning;

        bool IsCancellationRequested
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        [ExcludeFromCodeCoverage]
        public bool IsCombinationHeldDown
            => Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V);

        [ExcludeFromCodeCoverage]
        public bool IsOneCombinationKeyDown
            => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.V);

        [ExcludeFromCodeCoverage]
        CancellationToken Token
            => threadCancellationTokenSource.Token;

        [ExcludeFromCodeCoverage]
        public int DurationInDeciseconds
            => 2;

        public void Connect(IWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already connected.");
            }

            InstallPasteHotkeyInterceptor();
        }

        [ExcludeFromCodeCoverage]
        async Task MonitorClipboardCombinationStateAsync()
        {
            await WaitForCombinationRelease();
            if (IsCancellationRequested)
            {
                return;
            }

            RegisterCombinationReleased();
        }

        [ExcludeFromCodeCoverage]
        void RegisterCombinationReleased()
        {
            isCombinationDown = false;
            if (PasteCombinationReleased != null)
            {
                mainThreadInvoker.Invoke(
                                         () =>
                                         PasteCombinationReleased(
                                                                  this,
                                                                  new PasteCombinationReleasedEventArgument
                                                                      ()));
            }
        }

        [ExcludeFromCodeCoverage]
        async Task WaitForCombinationRelease()
        {
            var decisecondsPassed = 0;
            while (!IsCancellationRequested && IsOneCombinationKeyDown)
            {
                await threadDelay.ExecuteAsync(100);
                decisecondsPassed++;

                logger.Information($"Paste combination held down for {decisecondsPassed}.");

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
            }
        }

        [ExcludeFromCodeCoverage]
        void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if (decisecondsPassed != DurationInDeciseconds || PasteCombinationDurationPassed == null)
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

        [ExcludeFromCodeCoverage]
        void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        [ExcludeFromCodeCoverage]
        void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired -= PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        [ExcludeFromCodeCoverage]
        void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, HotkeyFiredArgument e)
        {
            if (!isCombinationDown)
            {
                logger.Information(
                                   "Paste combination duration mediator reacted to paste hotkey.",
                                   1);

                isCombinationDown = true;
                threadLoop.Notify(MonitorClipboardCombinationStateAsync, Token);
            }
            else
            {
                logger.Information(
                                   "Paste combination duration mediator ignored paste hotkey because the paste combination was already held down.",
                                   1);
            }
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