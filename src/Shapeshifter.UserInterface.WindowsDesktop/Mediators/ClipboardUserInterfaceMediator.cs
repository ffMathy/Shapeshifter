using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardUserInterfaceMediator :
        IClipboardUserInterfaceMediator
    {
        private readonly IClipboardCopyInterceptor clipboardHook;
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
            IClipboardCopyInterceptor clipboardHook,
            IPasteCombinationDurationMediator pasteCombinationDurationMediator,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory)
        {
            this.clipboardHook = clipboardHook;
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
            var package = clipboardDataControlPackageFactory.Create(e.Data);
            clipboardPackages.Add(package);

            //signal an added event.
            if (ControlAdded != null)
            {
                ControlAdded(this, new ControlEventArgument(package));
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

            pasteCombinationDurationMediator.PasteCombinationDurationPassed -= PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased -= PasteCombinationDurationMediator_PasteCombinationReleased;
        }

        private void UninstallClipboardHook()
        {
            clipboardHook.DataCopied -= ClipboardHook_DataCopied;
        }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The user interface mediator is already connected.");
            }

            InstallClipboardHook();
            InstallPastecombinationDurationMediator();
        }

        private void InstallPastecombinationDurationMediator()
        {
            pasteCombinationDurationMediator.PasteCombinationDurationPassed += PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased += PasteCombinationDurationMediator_PasteCombinationReleased;

            pasteCombinationDurationMediator.Connect();
        }

        private void InstallClipboardHook()
        {
            clipboardHook.DataCopied += ClipboardHook_DataCopied;
        }

        private void PasteCombinationDurationMediator_PasteCombinationDurationPassed(
            object sender, 
            PasteCombinationDurationPassedEventArgument e)
        {
            if(UserInterfaceShown != null)
            {
                UserInterfaceShown(this, new UserInterfaceShownEventArgument());
            }
        }

        private void PasteCombinationDurationMediator_PasteCombinationReleased(
            object sender,
            PasteCombinationReleasedEventArgument e)
        {
            if(UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, new UserInterfaceHiddenEventArgument());
            }
        }
    }
}
