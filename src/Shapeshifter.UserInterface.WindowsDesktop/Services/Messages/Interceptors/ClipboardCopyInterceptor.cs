using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System.Runtime.InteropServices;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class ClipboardCopyInterceptor : IClipboardCopyInterceptor
    {
        public event EventHandler<DataCopiedEventArgument> DataCopied;

        uint lastClipboardItemIdentifier;
        bool shouldSkipNext;

        readonly ILogger logger;

        public ClipboardCopyInterceptor(
            ILogger logger)
        {
            this.logger = logger;
        }

        void HandleClipboardUpdateWindowMessage()
        {
            var clipboardItemIdentifier = ClipboardApi.GetClipboardSequenceNumber();

            logger.Information($"Clipboard update message received with sequence #{clipboardItemIdentifier}.", 1);

            if (clipboardItemIdentifier != lastClipboardItemIdentifier)
            {
                lastClipboardItemIdentifier = clipboardItemIdentifier;

                TriggerDataCopiedEvent();
            }
        }

        void TriggerDataCopiedEvent()
        {
            if (DataCopied != null)
            {
                var thread = new Thread(() => DataCopied(this, new DataCopiedEventArgument()));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        public void Install(IntPtr windowHandle)
        {
            if (!ClipboardApi.AddClipboardFormatListener(windowHandle))
            {
                throw GenerateInstallFailureException();
            }
        }

        static Exception GenerateInstallFailureException()
        {
            var errorCode = Marshal.GetLastWin32Error();

            var existingOwner = ClipboardApi.GetClipboardOwner();
            var ownerTitle = WindowApi.GetWindowTitle(existingOwner);

            return new InvalidOperationException($"Could not install a clipboard hook for the main window. The window '{ownerTitle}' currently owns the clipboard. The last error code was {errorCode}.");
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
                if (shouldSkipNext)
                {
                    logger.Information("Clipboard update message skipped.");

                    shouldSkipNext = false;
                    return;
                }

                HandleClipboardUpdateWindowMessage();
            }
        }

        public void SkipNext()
        {
            shouldSkipNext = true;
        }
    }
}
