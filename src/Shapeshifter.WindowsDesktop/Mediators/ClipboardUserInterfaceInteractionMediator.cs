namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    using Controls.Clipboard.Factories.Interfaces;
    using Controls.Window.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Services.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interceptors.Interfaces;

    class ClipboardUserInterfaceInteractionMediator:
        IClipboardUserInterfaceInteractionMediator
    {
        readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        readonly IPasteCombinationDurationMediator pasteCombinationDurationMediator;
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;
        readonly IKeyInterceptor hotkeyInterceptor;
        readonly IMouseWheelHook mouseWheelHook;

        readonly IList<IClipboardDataControlPackage> clipboardPackages;

        public event EventHandler<PackageEventArgument> PackageAdded;
        public event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        public event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;
        public event EventHandler<PastePerformedEventArgument> PastePerformed;
        public event EventHandler SelectedNextItem;
        public event EventHandler SelectedPreviousItem;

        public ClipboardUserInterfacePane CurrentPane { get; set; }

        public bool IsConnected
            => pasteCombinationDurationMediator.IsConnected;

        public IEnumerable<IClipboardDataControlPackage> ClipboardElements
            => clipboardPackages;

        public void Cancel()
        {
            pasteCombinationDurationMediator.CancelCombinationRegistration();
        }

        public ClipboardUserInterfaceInteractionMediator(
            IClipboardCopyInterceptor clipboardCopyInterceptor,
            IPasteCombinationDurationMediator pasteCombinationDurationMediator,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory,
            IKeyInterceptor hotkeyInterceptor,
            IMouseWheelHook mouseWheelHook)
        {
            this.clipboardCopyInterceptor = clipboardCopyInterceptor;
            this.pasteCombinationDurationMediator = pasteCombinationDurationMediator;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.clipboardDataControlPackageFactory = clipboardDataControlPackageFactory;
            this.hotkeyInterceptor = hotkeyInterceptor;
            this.mouseWheelHook = mouseWheelHook;

            clipboardPackages = new List<IClipboardDataControlPackage>();

            SetupHotkeyInterceptor();
            SetupMouseHook();
        }

        public void SetupHotkeyInterceptor()
        {
            hotkeyInterceptor.HotkeyFired += HotkeyInterceptor_HotkeyFired;
        }

        void HotkeyInterceptor_HotkeyFired(
            object sender,
            HotkeyFiredArgument e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    OnSelectedNextItem();
                    break;

                case Key.Up:
                    OnSelectedPreviousItem();
                    break;

                case Key.Left:
                    HandleLeftPressed();
                    break;

                case Key.Right:
                    HandleRightPressed();
                    break;
            }
        }

        void SwapActivePane()
        {
            switch (CurrentPane)
            {
                case ClipboardUserInterfacePane.Actions:
                    CurrentPane = ClipboardUserInterfacePane.ClipboardPackages;
                    break;

                case ClipboardUserInterfacePane.ClipboardPackages:
                    CurrentPane = ClipboardUserInterfacePane.Actions;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unknown user interface pane found.");
            }
        }

        void HandleLeftPressed()
        {
            if (CurrentPane == ClipboardUserInterfacePane.ClipboardPackages)
            {
                Cancel();
            }
            else
            {
                SwapActivePane();
            }
        }

        void HandleRightPressed()
        {
            if (CurrentPane == ClipboardUserInterfacePane.Actions)
            {
                Cancel();
            }
            else
            {
                SwapActivePane();
            }
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
            PackageAdded?.Invoke(this, new PackageEventArgument(package));
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
            UninstallMouseWheelHook();
        }

        void UninstallMouseWheelHook()
        {
            mouseWheelHook.WheelScrolledDown -= MouseWheelHookOnScrolledDown;
            mouseWheelHook.WheelScrolledUp -= MouseWheelHookOnScrolledUp;
            mouseWheelHook.WheelTilted -= MouseWheelHook_WheelTilted;
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

        void SetupMouseHook()
        {
            mouseWheelHook.WheelScrolledDown += MouseWheelHookOnScrolledDown;
            mouseWheelHook.WheelScrolledUp += MouseWheelHookOnScrolledUp;
            mouseWheelHook.WheelTilted += MouseWheelHook_WheelTilted;
        }

        void MouseWheelHook_WheelTilted(object sender, EventArgs e)
        {
            SwapActivePane();
        }

        void MouseWheelHookOnScrolledUp(object sender, EventArgs eventArgs)
        {
            OnSelectedPreviousItem();
        }

        void MouseWheelHookOnScrolledDown(object sender, EventArgs eventArgs)
        {
            OnSelectedNextItem();
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
            ResetCurrentPane();
            UserInterfaceHidden?.Invoke(
                this,
                new UserInterfaceHiddenEventArgument());
            mouseWheelHook.ResetAccumulatedWheelDelta();
        }

        void ResetCurrentPane()
        {
            CurrentPane = ClipboardUserInterfacePane.ClipboardPackages;
        }

        protected virtual void OnSelectedNextItem()
        {
            SelectedNextItem?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSelectedPreviousItem()
        {
            SelectedPreviousItem?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPackageAdded(PackageEventArgument e)
        {
            PackageAdded?.Invoke(this, e);
        }

        protected virtual void OnUserInterfaceShown(
            UserInterfaceShownEventArgument e)
        {
            UserInterfaceShown?.Invoke(this, e);
        }

        protected virtual void OnUserInterfaceHidden(
            UserInterfaceHiddenEventArgument e)
        {
            UserInterfaceHidden?.Invoke(this, e);
        }

        protected virtual void OnPastePerformed(
            PastePerformedEventArgument e)
        {
            PastePerformed?.Invoke(this, e);
        }
    }
}