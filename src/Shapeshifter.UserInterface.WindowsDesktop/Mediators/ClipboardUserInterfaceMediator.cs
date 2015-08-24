using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardUserInterfaceMediator :
        IClipboardUserInterfaceMediator
    {
        readonly IClipboardCopyInterceptor clipboardHook;
        readonly IPasteCombinationDurationMediator pasteCombinationDurationMediator;
        readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;
        readonly IClipboardPasteService clipboardPasteService;

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
            IClipboardCopyInterceptor clipboardHook,
            IPasteCombinationDurationMediator pasteCombinationDurationMediator,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory,
            IClipboardPasteService clipboardPasteService)
        {
            this.clipboardHook = clipboardHook;
            this.pasteCombinationDurationMediator = pasteCombinationDurationMediator;
            this.clipboardDataControlPackageFactory = clipboardDataControlPackageFactory;
            this.clipboardPasteService = clipboardPasteService;

            clipboardPackages = new List<IClipboardDataControlPackage>();
        }

        void OnPasteCombinationHeldDownForLongTime()
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, new UserInterfaceShownEventArgument());
            }
        }

        void ClipboardHook_DataCopied(
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

        void UninstallPasteHotkeyInterceptor()
        {
            pasteCombinationDurationMediator.Disconnect();

            pasteCombinationDurationMediator.PasteCombinationDurationPassed -= PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased -= PasteCombinationDurationMediator_PasteCombinationReleased;
        }

        void UninstallClipboardHook()
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

        void InstallPastecombinationDurationMediator()
        {
            pasteCombinationDurationMediator.PasteCombinationDurationPassed += PasteCombinationDurationMediator_PasteCombinationDurationPassed;
            pasteCombinationDurationMediator.PasteCombinationReleased += PasteCombinationDurationMediator_PasteCombinationReleased;

            pasteCombinationDurationMediator.Connect();
        }

        void InstallClipboardHook()
        {
            clipboardHook.DataCopied += ClipboardHook_DataCopied;
        }

        void PasteCombinationDurationMediator_PasteCombinationDurationPassed(
            object sender,
            PasteCombinationDurationPassedEventArgument e)
        {
            RaiseUserInterfaceShownEvent();
        }

        void RaiseUserInterfaceShownEvent()
        {
            if (UserInterfaceShown != null)
            {
                UserInterfaceShown(this, new UserInterfaceShownEventArgument());
            }
        }

        void PasteCombinationDurationMediator_PasteCombinationReleased(
            object sender,
            PasteCombinationReleasedEventArgument e)
        {
            RaiseUserInterfaceHiddenEvent();
            SimulatePaste();
        }

        void SimulatePaste()
        {
            clipboardPasteService.PasteClipboardContents();
        }

        void RaiseUserInterfaceHiddenEvent()
        {
            if (UserInterfaceHidden != null)
            {
                UserInterfaceHidden(this, new UserInterfaceHiddenEventArgument());
            }
        }
    }
}
