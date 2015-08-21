using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class ClipboardHookService : IClipboardHookService
    {

        public event EventHandler<DataCopiedEventArgument> DataCopied;

        private uint lastClipboardItemIdentifier;

        private readonly IWindowMessageHook windowMessageHook;

        public ClipboardHookService(
            IWindowMessageHook windowMessageHook)
        {
            this.windowMessageHook = windowMessageHook;
        }

        public bool IsConnected
        {
            get; private set;
        }

        public void Connect()
        {
            EnsureWindowIsPresent();

            if (!IsConnected)
            {
                InstallHooks();
                IsConnected = true;
            }
        }

        private void InstallHooks()
        {
            InstallClipboardHook();
            InstallWindowMessageHook();
        }

        private static void EnsureWindowIsPresent()
        {
            var mainWindow = App.Current.MainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Can't install a clipboard hook when there is no window open.");
            }
        }

        private void InstallWindowMessageHook()
        {
            windowMessageHook.MessageReceived += WindowMessageHook_MessageReceived;
            windowMessageHook.Connect();
        }

        private void WindowMessageHook_MessageReceived(object sender, WindowMessageReceivedArgument e)
        {
            if (e.Message == ClipboardApi.WM_CLIPBOARDUPDATE)
            {
                HandleClipboardUpdateWindowMessage();
            }
        }

        private void InstallClipboardHook()
        {
            var windowHandle = windowMessageHook.MainWindowHandle;
            if (!ClipboardApi.AddClipboardFormatListener(windowHandle))
            {
                throw new NotImplementedException("Could not install a clipboard hook for the main window.");
            }
        }

        private void HandleClipboardUpdateWindowMessage()
        {
            var clipboardItemIdentifier = ClipboardApi.GetClipboardSequenceNumber();
            if (clipboardItemIdentifier != lastClipboardItemIdentifier)
            {
                lastClipboardItemIdentifier = clipboardItemIdentifier;

                TriggerDataCopiedEvent();
            }
        }

        private void TriggerDataCopiedEvent()
        {
            if (DataCopied != null)
            {
                var data = WindowsClipboard.GetDataObject();
                if (data != null)
                {
                    DataCopied(this, new DataCopiedEventArgument(data));
                }
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                UninstallWindowMessageHook();

                IsConnected = false;
            }
        }

        private void UninstallWindowMessageHook()
        {
            //TODO: we can't disconnect the window message hook here as others may be listening - what else can we do?
            windowMessageHook.MessageReceived -= WindowMessageHook_MessageReceived;
        }
    }
}
