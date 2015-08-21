using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class ClipboardCopyInterceptor : IClipboardCopyInterceptor
    {

        public event EventHandler<DataCopiedEventArgument> DataCopied;

        private uint lastClipboardItemIdentifier;

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

        public void Install(IntPtr windowHandle)
        {
            if (!ClipboardApi.AddClipboardFormatListener(windowHandle))
            {
                throw new InvalidOperationException("Could not install a clipboard hook for the main window.");
            }
        }

        public void Uninstall(IntPtr windowHandle)
        {
            if (!ClipboardApi.RemoveClipboardFormatListener(windowHandle))
            {
                throw new InvalidOperationException("Could not uninstall a clipboard hook for the main window.");
            }
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument eventArgument)
        {
            if (eventArgument.Message == ClipboardApi.WM_CLIPBOARDUPDATE)
            {
                HandleClipboardUpdateWindowMessage();
            }
        }
    }
}
