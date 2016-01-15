namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Collections.Generic;

    using Controls.Clipboard.Factories.Interfaces;
    using Controls.Window.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interceptors.Interfaces;

    class ClipboardUserInterfaceMediator:
        IClipboardUserInterfaceMediator
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        readonly IPasteCombinationDurationMediator pasteCombinationDurationMediator;
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;

        readonly IList<IClipboardDataControlPackage> clipboardPackages;

        public event EventHandler<ControlEventArgument> ControlAdded;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public event EventHandler<PastePerformedEventArgument> PastePerformed;

        public bool IsConnected
            => pasteCombinationDurationMediator.IsConnected;

        public IEnumerable<IClipboardDataControlPackage> ClipboardElements
            => clipboardPackages;

        public void Cancel()
        {
            pasteCombinationDurationMediator.CancelCombinationRegistration();
        }

        public ClipboardUserInterfaceMediator(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IPasteCombinationDurationMediator pasteCombinationDurationMediator,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.pasteCombinationDurationMediator = pasteCombinationDurationMediator;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.clipboardDataControlPackageFactory = clipboardDataControlPackageFactory;

            clipboardPackages = new List<IClipboardDataControlPackage>();
        }

        void ClipboardHook_DataCopied(
            object sender,
            DataCopiedEventArgument e)
        {
            AppendPackagesWithDataFromClipboard();
        }

        void AppendPackagesWithDataFromClipboard()
        {
            var package = clipboardDataControlPackageFactory.CreateFromCurrentClipboardData();
            if (package == null)
            {
                return;
            }

            clipboardPackages.Add(package);

            FireControlAddedEvent(package);
        }

        void FireControlAddedEvent(IClipboardDataControlPackage package)
        {
            ControlAdded?.Invoke(this, new ControlEventArgument(package));
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "The user interface mediator is already disconnected.");
            }

            UninstallClipboardHook();
            UninstallPasteHotkeyInterceptor();
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteCombinationDurationMediator.Disconnect();

            pasteCombinationDurationMediator.PasteCombinationDurationPassed -=
                PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased -=
                PasteCombinationDurationMediatorPasteCombinationReleased;
            pasteCombinationDurationMediator.AfterPasteCombinationReleased -=
                AfterPasteCombinationDurationMediatorAfterPasteCombinationReleased;
        }

        void AfterPasteCombinationDurationMediatorAfterPasteCombinationReleased(
            object sender,
            PasteCombinationReleasedEventArgument e)
        {
            RaisePastePerformedEvent();
        }

        void RaisePastePerformedEvent()
        {
            PastePerformed?.Invoke(this, new PastePerformedEventArgument());
        }

        void UninstallClipboardHook()
        {
            clipboardCopyInterceptor.DataCopied -= ClipboardHook_DataCopied;
        }

        public void Connect(
            IHookableWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The user interface mediator is already connected.");
            }

            LoadInitialClipboardData();
            InstallClipboardHook();
            InstallPasteCombinationDurationMediator(targetWindow);
        }

        void LoadInitialClipboardData()
        {
            AppendPackagesWithDataFromClipboard();
        }

        void InstallPasteCombinationDurationMediator(
            IHookableWindow targetWindow)
        {
            pasteCombinationDurationMediator.PasteCombinationDurationPassed +=
                PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased +=
                PasteCombinationDurationMediatorPasteCombinationReleased;
            pasteCombinationDurationMediator.AfterPasteCombinationReleased +=
                AfterPasteCombinationDurationMediatorAfterPasteCombinationReleased;

            pasteCombinationDurationMediator.Connect(targetWindow);
        }

        void InstallClipboardHook()
        {
            clipboardCopyInterceptor.DataCopied += ClipboardHook_DataCopied;
        }

        void PasteCombinationDurationMediator_PasteCombinationDurationPassed(
            object sender,
            PasteCombinationDurationPassedEventArgument e)
        {
            RaiseUserInterfaceShownEvent();
        }

        void RaiseUserInterfaceShownEvent()
        {
            pasteHotkeyInterceptor.SkipNext();
            UserInterfaceShown?.Invoke(this, new UserInterfaceShownEventArgument());
        }

        void PasteCombinationDurationMediatorPasteCombinationReleased(
            object sender,
            PasteCombinationReleasedEventArgument e)
        {
            RaiseUserInterfaceHiddenEvent();
        }

        void RaiseUserInterfaceHiddenEvent()
        {
            UserInterfaceHidden?.Invoke(this, new UserInterfaceHiddenEventArgument());
        }
    }
}