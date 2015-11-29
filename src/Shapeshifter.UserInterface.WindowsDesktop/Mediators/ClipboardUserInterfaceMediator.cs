namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    using System;
    using System.Collections.Generic;

    using Windows.Interfaces;

    using Data.Interfaces;

    using Factories.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Services.Messages.Interceptors.Interfaces;

    class ClipboardUserInterfaceMediator:
        IClipboardUserInterfaceMediator
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;

        readonly IPasteCombinationDurationMediator pasteCombinationDurationMediator;

        readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;

        readonly IList<IClipboardDataControlPackage> clipboardPackages;

        public event EventHandler<ControlEventArgument> ControlAdded;

        public event EventHandler<ControlEventArgument> ControlRemoved;

        public event EventHandler<ControlEventArgument> ControlPinned;

        public event EventHandler<ControlEventArgument> ControlHighlighted;

        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        public bool IsConnected
            => pasteCombinationDurationMediator.IsConnected;

        public IEnumerable<IClipboardDataControlPackage> ClipboardElements
            => clipboardPackages;

        public ClipboardUserInterfaceMediator(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IPasteCombinationDurationMediator pasteCombinationDurationMediator,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.pasteCombinationDurationMediator = pasteCombinationDurationMediator;
            this.clipboardDataControlPackageFactory = clipboardDataControlPackageFactory;

            clipboardPackages = new List<IClipboardDataControlPackage>();
        }

        void ClipboardHook_DataCopied(
            object sender,
            DataCopiedEventArgument e)
        {
            var package = clipboardDataControlPackageFactory.Create();
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
                PasteCombinationDurationMediator_PasteCombinationReleased;
        }

        void UninstallClipboardHook()
        {
            clipboardCopyInterceptor.DataCopied -= ClipboardHook_DataCopied;
        }

        public void Connect(IWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The user interface mediator is already connected.");
            }

            InstallClipboardHook();
            InstallPastecombinationDurationMediator(targetWindow);
        }

        void InstallPastecombinationDurationMediator(IWindow targetWindow)
        {
            pasteCombinationDurationMediator.PasteCombinationDurationPassed +=
                PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased +=
                PasteCombinationDurationMediator_PasteCombinationReleased;

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
            UserInterfaceShown?.Invoke(this, new UserInterfaceShownEventArgument());
        }

        void PasteCombinationDurationMediator_PasteCombinationReleased(
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