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
        private readonly IClipboardHookService clipboardHook;
        private readonly IClipboardCombinationMediator clipboardCombinationMediator;
        private readonly IClipboardDataControlPackageFactory clipboardDataControlPackageFactory;

        private readonly IList<IClipboardDataControlPackage> clipboardPackages;

        public event EventHandler<ControlEventArgument> ControlAdded;
        public event EventHandler<ControlEventArgument> ControlRemoved;
        public event EventHandler<ControlEventArgument> ControlPinned;
        public event EventHandler<ControlEventArgument> ControlHighlighted;

        public bool IsConnected => clipboardHook.IsConnected && clipboardCombinationMediator.IsConnected;

        public IEnumerable<IClipboardDataControlPackage> ClipboardElements => clipboardPackages;

        public ClipboardUserInterfaceMediator(
            IClipboardHookService clipboardHook,
            IClipboardCombinationMediator clipboardCombinationMediator,
            IClipboardDataControlPackageFactory clipboardDataControlPackageFactory)
        {
            this.clipboardHook = clipboardHook;
            this.clipboardCombinationMediator = clipboardCombinationMediator;
            this.clipboardDataControlPackageFactory = clipboardDataControlPackageFactory;

            clipboardPackages = new List<IClipboardDataControlPackage>();
        }

        private void ClipboardHook_DataCopied(object sender, DataCopiedEventArgument e)
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
            clipboardCombinationMediator.Disconnect();
        }

        private void UninstallClipboardHook()
        {
            clipboardHook.DataCopied -= ClipboardHook_DataCopied;
            clipboardHook.Disconnect();
        }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The user interface mediator is already connected.");
            }

            InstallClipboardHook();
            InstallPasteHotkeyInterceptor();
        }

        private void InstallPasteHotkeyInterceptor()
        {
            clipboardCombinationMediator.Connect();
        }

        private void InstallClipboardHook()
        {
            clipboardHook.Connect();
            clipboardHook.DataCopied += ClipboardHook_DataCopied;
        }
    }
}
