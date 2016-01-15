namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Windows.Input;

    using Factories.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    class PasteHotkeyInterceptor: IPasteHotkeyInterceptor
    {
        readonly ILogger logger;

        readonly IHotkeyInterception hotkeyInterception;

        IntPtr mainWindowHandle;

        bool isInstalled;
        bool shouldSkipNext;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

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

            HotkeyFired?.Invoke(
                this,
                new HotkeyFiredArgument(
                    hotkeyInterception.Key,
                    hotkeyInterception.ControlNeeded));
        }

        public void SkipNext()
        {
            shouldSkipNext = true;
        }

        public bool IsEnabled { get; set; }
    }
}