namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    class ClipboardCopyInterceptor: IClipboardCopyInterceptor
    {
        public event EventHandler<DataCopiedEventArgument> DataCopied;

        uint lastClipboardItemIdentifier;

        bool shouldSkipNext;

        IntPtr mainWindowHandle;

        readonly ILogger logger;

        readonly IClipboardNativeApi clipboardNativeApi;

        readonly IWindowNativeApi windowNativeApi;

        public ClipboardCopyInterceptor(
            ILogger logger,
            IClipboardNativeApi clipboardNativeApi,
            IWindowNativeApi windowNativeApi)
        {
            this.logger = logger;
            this.clipboardNativeApi = clipboardNativeApi;
            this.windowNativeApi = windowNativeApi;
        }

        void HandleClipboardUpdateWindowMessage()
        {
            var clipboardItemIdentifier = clipboardNativeApi.GetClipboardSequenceNumber();

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
            if (!clipboardNativeApi.AddClipboardFormatListener(windowHandle))
            {
                throw GenerateInstallFailureException();
            }
        }

        Exception GenerateInstallFailureException()
        {
            var errorCode = Marshal.GetLastWin32Error();

            var existingOwner = clipboardNativeApi.GetClipboardOwner();
            var ownerTitle = windowNativeApi.GetWindowTitle(existingOwner);

            return
                new InvalidOperationException(
                    $"Could not install a clipboard hook for the main window. The window '{ownerTitle}' currently owns the clipboard. The last error code was {errorCode}.");
        }

        public void Uninstall()
        {
            if (!clipboardNativeApi.RemoveClipboardFormatListener(mainWindowHandle))
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