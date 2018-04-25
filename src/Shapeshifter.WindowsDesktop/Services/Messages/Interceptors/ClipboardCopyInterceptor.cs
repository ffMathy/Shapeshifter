namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
	using System;
	using System.Runtime.InteropServices;
	using System.Threading.Tasks;
	using Infrastructure.Events;

	using Interfaces;

	using Native.Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces;

	class ClipboardCopyInterceptor: IClipboardCopyInterceptor
    {
        public event EventHandler<DataCopiedEventArgument> DataCopied;

        uint lastClipboardItemIdentifier;

        bool shouldSkipNext;

        IntPtr mainWindowHandle;

        readonly ILogger logger;
        readonly IClipboardNativeApi clipboardNativeApi;
        readonly IWindowNativeApi windowNativeApi;
		readonly IThreadDeferrer threadDeferrer;

		public ClipboardCopyInterceptor(
            ILogger logger,
            IClipboardNativeApi clipboardNativeApi,
            IWindowNativeApi windowNativeApi,
			IThreadDeferrer threadDeferrer)
        {
            this.logger = logger;
            this.clipboardNativeApi = clipboardNativeApi;
            this.windowNativeApi = windowNativeApi;
			this.threadDeferrer = threadDeferrer;
		}

        async Task HandleClipboardUpdateWindowMessage()
        {
			await threadDeferrer.DeferAsync(200, () => {
				if (shouldSkipNext)
				{
					logger.Information("Clipboard update message skipped.");

					shouldSkipNext = false;
					return;
				}

				var clipboardItemIdentifier = clipboardNativeApi.GetClipboardSequenceNumber();

				logger.Information(
					$"Clipboard update message received with sequence #{clipboardItemIdentifier}.",
					1);

				if (clipboardItemIdentifier == lastClipboardItemIdentifier)
				{
					logger.Verbose("Skipping clipboard update message because the sequence ID has not changed.");
					return;
				}

				lastClipboardItemIdentifier = clipboardItemIdentifier;

				TriggerDataCopiedEvent();
			});
        }

        void TriggerDataCopiedEvent()
        {
            DataCopied?.Invoke(this, new DataCopiedEventArgument());
        }

        public void Install(IntPtr windowHandle)
        {
            mainWindowHandle = windowHandle;
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

        public async Task ReceiveMessageEventAsync(WindowMessageReceivedArgument eventArgument)
        {
            if (eventArgument.Message != Message.WM_CLIPBOARDUPDATE)
            {
                return;
            }

            await HandleClipboardUpdateWindowMessage();
        }

        public void SkipNext()
        {
            shouldSkipNext = true;
        }
    }
}