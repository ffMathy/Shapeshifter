using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Threading;
using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    class PasteCombinationDurationMediator : IPasteCombinationDurationMediator
    {
        private readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        private readonly IThreadLoop threadLoop;
        private readonly IThreadDelay threadDelay;

        private readonly CancellationTokenSource threadCancellationTokenSource;
        private readonly ManualResetEventSlim threadCombinationHeldDownEvent;

        public event EventHandler<PasteCombinationDurationPassedEventArgument> PasteCombinationDurationPassed;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IThreadLoop threadLoop,
            IThreadDelay threadDelay)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.threadLoop = threadLoop;
            this.threadDelay = threadDelay;

            threadCancellationTokenSource = new CancellationTokenSource();
            threadCombinationHeldDownEvent = new ManualResetEventSlim();
        }

        public bool IsConnected 
            => threadLoop.IsRunning;

        private bool IsCancellationRequested 
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        public bool IsCombinationHeldDown 
            => Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V);

        private CancellationToken Token 
            => threadCancellationTokenSource.Token;

        public int DurationInDeciseconds { get; }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The clipboard combination mediator is already connected.");
            }

            InstallPasteHotkeyInterceptor();
            InstallThreadLoop();
        }

        private void InstallThreadLoop()
        {
            threadLoop.Start(MonitorClipboardCombinationState, Token);
        }

        private void MonitorClipboardCombinationState()
        {
            threadCombinationHeldDownEvent.Wait(Token);
            if (IsCancellationRequested) return;

            WaitForCombinationRelease();
            if (IsCancellationRequested) return;

            RegisterCombinationReleased();
        }

        private void RegisterCombinationReleased()
        {
            if (PasteCombinationReleased != null)
            {
                PasteCombinationReleased(this, new PasteCombinationReleasedEventArgument());
            }
        }

        private void WaitForCombinationRelease()
        {
            var decisecondsPassed = 0;
            while (!IsCancellationRequested && IsCombinationHeldDown)
            {
                threadDelay.Execute(100);
                decisecondsPassed++;

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
            }
        }

        private void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if (decisecondsPassed > DurationInDeciseconds && PasteCombinationDurationPassed != null)
            {
                PasteCombinationDurationPassed(this, new PasteCombinationDurationPassedEventArgument());
            }
        }

        private void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteHotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        private void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteHotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        private void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, PasteHotkeyFiredArgument e)
        {
            if (PasteCombinationDurationPassed != null)
            {
                PasteCombinationDurationPassed(this, new PasteCombinationDurationPassedEventArgument());
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
