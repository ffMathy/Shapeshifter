using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Threading;
using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    class ClipboardCombinationMediator : IClipboardCombinationMediator
    {
        private readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        private readonly IThreadLoop threadLoop;
        private readonly IThreadDelay threadDelay;

        private readonly CancellationTokenSource threadCancellationTokenSource;
        private readonly ManualResetEventSlim threadCombinationHeldDownEvent;

        public event EventHandler<PasteCombinationHeldDownEventArgument> PasteCombinationHeldDown;
        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        public ClipboardCombinationMediator(
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

        public bool IsConnected => pasteHotkeyInterceptor.IsConnected && threadLoop.IsRunning;

        private bool IsCancellationRequested => threadCancellationTokenSource.Token.IsCancellationRequested;

        public bool IsCombinationHeldDown => Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V);

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
            threadLoop.Start(MonitorClipboardCombinationState, GetToken());
        }

        private CancellationToken GetToken()
        {
            return threadCancellationTokenSource.Token;
        }

        private void MonitorClipboardCombinationState()
        {
            threadCombinationHeldDownEvent.Wait(GetToken());
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
            while (!IsCancellationRequested && IsCombinationHeldDown)
            {
                threadDelay.Execute(100);
            }
        }

        private void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteHotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
            pasteHotkeyInterceptor.Connect();
        }

        private void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, PasteHotkeyFiredArgument e)
        {
            if (PasteCombinationHeldDown != null)
            {
                PasteCombinationHeldDown(this, new PasteCombinationHeldDownEventArgument());
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The clipboard combination mediator is already disconnected.");
            }

            pasteHotkeyInterceptor.Disconnect();
        }
    }
}
