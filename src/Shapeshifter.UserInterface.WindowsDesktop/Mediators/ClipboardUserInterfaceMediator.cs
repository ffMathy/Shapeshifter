#region

using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators
{
    internal class ClipboardUserInterfaceMediator :
        IClipboardUserInterfaceMediator
    {
        private readonly IClipboardCopyInterceptor clipboardCopyInterceptor;
        private readonly IPasteCombinationDurationMediator pasteCombinationDurationMediator;
        private readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;

        private readonly IList<IClipboardDataControlPackage> clipboardPackages;

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

        private void OnPasteCombinationHeldDownForLongTime()
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, new UserInterfaceShownEventArgument());
            }
        }

        private void ClipboardHook_DataCopied(
            object sender,
            DataCopiedEventArgument e)
        {
            var package = clipboardDataControlPackageFactory.Create();
            if (package != null)
            {
                clipboardPackages.Add(package);

                FireControlAddedEvent(package);
            }
        }

        private void FireControlAddedEvent(IClipboardDataControlPackage package)
        {
            if (ControlAdded != null)
            {
                ControlAdded(this, new ControlEventArgument(package));
                //userInterfaceThread.Invoke(() => ControlAdded(this, new ControlEventArgument(package)));
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The user interface mediator is already disconnected.");
            }

            UninstallClipboardHook();
            UninstallPasteHotkeyInterceptor();
        }

        private void UninstallPasteHotkeyInterceptor()
        {
            pasteCombinationDurationMediator.Disconnect();

            pasteCombinationDurationMediator.PasteCombinationDurationPassed -=
                PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased -=
                PasteCombinationDurationMediator_PasteCombinationReleased;
        }

        private void UninstallClipboardHook()
        {
            clipboardCopyInterceptor.DataCopied -= ClipboardHook_DataCopied;
        }

        public void Connect(IWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The user interface mediator is already connected.");
            }

            InstallClipboardHook();
            InstallPastecombinationDurationMediator(targetWindow);
        }

        private void InstallPastecombinationDurationMediator(IWindow targetWindow)
        {
            pasteCombinationDurationMediator.PasteCombinationDurationPassed +=
                PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased +=
                PasteCombinationDurationMediator_PasteCombinationReleased;

            pasteCombinationDurationMediator.Connect(targetWindow);
        }

        private void InstallClipboardHook()
        {
            clipboardCopyInterceptor.DataCopied += ClipboardHook_DataCopied;
        }

        private void PasteCombinationDurationMediator_PasteCombinationDurationPassed(
            object sender,
            PasteCombinationDurationPassedEventArgument e)
        {
            RaiseUserInterfaceShownEvent();
        }

        private void RaiseUserInterfaceShownEvent()
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, new UserInterfaceShownEventArgument());
            }
        }

        private void PasteCombinationDurationMediator_PasteCombinationReleased(
            object sender,
            PasteCombinationReleasedEventArgument e)
        {
            RaiseUserInterfaceHiddenEvent();
        }

        private void RaiseUserInterfaceHiddenEvent()
        {
            if (UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, new UserInterfaceHiddenEventArgument());
            }
        }
    }
}