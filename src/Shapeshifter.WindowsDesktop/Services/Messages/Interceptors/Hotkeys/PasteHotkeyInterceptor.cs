namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
	using System;
	using System.Windows.Input;

	using Factories.Interfaces;

	using Infrastructure.Events;

	using Interfaces;
	using Serilog;

	class PasteHotkeyInterceptor: IPasteHotkeyInterceptor
    {
        readonly ILogger logger;
        readonly IHotkeyInterception hotkeyInterception;

        IntPtr mainWindowHandle;

        bool isInstalled;
        bool shouldSkipNext;

        public event EventHandler PasteDetected;

        public bool IsEnabled { get; set; }

        public PasteHotkeyInterceptor(
            ILogger logger,
            IHotkeyInterceptionFactory hotkeyInterceptionFactory)
        {
            this.logger = logger;

            IsEnabled = true;

            hotkeyInterception = hotkeyInterceptionFactory.CreateInterception(
                Key.V,
                true,
                true);
        }

        public void Install(IntPtr windowHandle)
        {
            if (isInstalled)
            {
                throw new InvalidOperationException("This interceptor has already been installed.");
            }

            mainWindowHandle = windowHandle;
            hotkeyInterception.Start(windowHandle);

            isInstalled = true;
        }

        public void Uninstall()
        {
            if (!isInstalled)
            {
                throw new InvalidOperationException(
                    "This interceptor has already been uninstalled.");
            }

            hotkeyInterception.Stop(mainWindowHandle);

            isInstalled = false;
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if ((e.Message == Message.WM_HOTKEY) &&
                ((int) e.WordParameter == hotkeyInterception.InterceptionId))
            {
                HandleHotkeyMessage();
            }
        }

        void HandleHotkeyMessage()
        {
            if (!IsEnabled)
            {
                logger.Information("Skipped paste hotkey message because the interceptor is disabled.");
                return;
            }

            if (shouldSkipNext)
            {
                shouldSkipNext = false;
                return;
            }

            logger.Information("Paste hotkey message received.", 1);

            OnPasteDetected();
        }

        public void SkipNext()
        {
            shouldSkipNext = true;
        }

        protected virtual void OnPasteDetected()
        {
            PasteDetected?.Invoke(this, new EventArgs());
        }
    }
}