using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Threading;
using System.Windows.Input;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    class PasteCombinationDurationMediator : IPasteCombinationDurationMediator
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IThreadLoop threadLoop;
        readonly IThreadDelay threadDelay;
        readonly ILogger logger;
        readonly IMainThreadInvoker mainThreadInvoker;

        readonly CancellationTokenSource threadCancellationTokenSource;
        readonly ManualResetEventSlim threadCombinationHeldDownEvent;

        bool isCombinationDown;

        public event EventHandler<PasteCombinationDurationPassedEventArgument> PasteCombinationDurationPassed;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IThreadLoop threadLoop,
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
            threadCombinationHeldDownEvent = new ManualResetEventSlim();
        }

        public bool IsConnected 
            => threadLoop.IsRunning;

        bool IsCancellationRequested
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        public bool IsCombinationHeldDown 
            => Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V);

        public bool IsOneCombinationKeyDown
            => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.V);

        CancellationToken Token
            => threadCancellationTokenSource.Token;

        public int DurationInDeciseconds
            => 2;

        public void Connect(IWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The clipboard combination mediator is already connected.");
            }

            InstallPasteHotkeyInterceptor();
            InstallThreadLoop();
        }

        void InstallThreadLoop()
        {
            threadLoop.Start(MonitorClipboardCombinationState, Token);
            logger.Information("Clipboard combination monitoring loop installed.", 1);
        }

        void MonitorClipboardCombinationState()
        {
            threadCombinationHeldDownEvent.Wait(Token);
            threadCombinationHeldDownEvent.Reset();

            if (IsCancellationRequested) return;

            WaitForCombinationRelease();
            if (IsCancellationRequested) return;

            RegisterCombinationReleased();
        }

        void RegisterCombinationReleased()
        {
            isCombinationDown = false;
            if (PasteCombinationReleased != null)
            {
                mainThreadInvoker.Invoke(() => PasteCombinationReleased(this, new PasteCombinationReleasedEventArgument()));
            }
        }

        void WaitForCombinationRelease()
        {
            var decisecondsPassed = 0;
            while (!IsCancellationRequested && IsOneCombinationKeyDown)
            {
                threadDelay.Execute(100);
                decisecondsPassed++;

                logger.Information($"Paste combination held down for {decisecondsPassed}.");

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
            }
        }

        void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if (decisecondsPassed == DurationInDeciseconds && PasteCombinationDurationPassed != null)
            {
                mainThreadInvoker.Invoke(() => PasteCombinationDurationPassed(this, new PasteCombinationDurationPassedEventArgument()));
                logger.Information("Paste duration passed event raised.");
            }
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
            if (!isCombinationDown)
            {
                logger.Information("Paste combination duration mediator reacted to paste hotkey.", 1);

                isCombinationDown = true;
                threadCombinationHeldDownEvent.Set();
            } else
            {
                logger.Information("Paste combination duration mediator ignored paste hotkey because the paste combination was already held down.", 1);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The clipboard combination mediator is already disconnected.");
            }

            UninstallPasteHotkeyInterceptor();
        }
    }
}
