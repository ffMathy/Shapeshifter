namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Native;

    class ClipboardCopyInterceptor: IClipboardCopyInterceptor
    {
        public event EventHandler<DataCopiedEventArgument> DataCopied;

        uint lastClipboardItemIdentifier;

        bool shouldSkipNext;

        IntPtr mainWindowHandle;

        readonly ILogger logger;

        public ClipboardCopyInterceptor(
            ILogger logger)
        {
            this.logger = logger;
        }

        void HandleClipboardUpdateWindowMessage()
        {
            var clipboardItemIdentifier = ClipboardApi.GetClipboardSequenceNumber();

            logger.Information(
                $"Clipboard update message received with sequence #{clipboardItemIdentifier}.",
                1);

            if (clipboardItemIdentifier == lastClipboardItemIdentifier)
            {
                return;
            }

            lastClipboardItemIdentifier = clipboardItemIdentifier;

            TriggerDataCopiedEvent();
        }

        void TriggerDataCopiedEvent()
        {
            if (DataCopied == null)
            {
                return;
            }

            var thread = new Thread(() => DataCopied(this, new DataCopiedEventArgument()))
            {
                IsBackground = true
            };
            thread.Start();
        }

        public void Install(IntPtr windowHandle)
        {
            this.mainWindowHandle = windowHandle;
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

            return
                new InvalidOperationException(
                    $"Could not install a clipboard hook for the main window. The window '{ownerTitle}' currently owns the clipboard. The last error code was {errorCode}.");
        }

        public void Uninstall()
        {
            if (!ClipboardApi.RemoveClipboardFormatListener(mainWindowHandle))
            {
                throw new InvalidOperationException(
                    "Could not uninstall a clipboard hook for the main window.");
            }
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument eventArgument)
        {
            if (eventArgument.Message != Message.WM_CLIPBOARDUPDATE)
            {
                return;
            }

            if (shouldSkipNext)
            {
                logger.Information("Clipboard update message skipped.");

                shouldSkipNext = false;
                return;
            }

            HandleClipboardUpdateWindowMessage();
        }

        public void SkipNext()
        {
            shouldSkipNext = true;
        }
    }
}