namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;

    using Factories.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Native;

    class PasteHotkeyInterceptor: IPasteHotkeyInterceptor
    {
        readonly ILogger logger;

        readonly IHotkeyInterception hotkeyInterception;

        IntPtr mainWindowHandle;

        bool isInstalled;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

        public PasteHotkeyInterceptor(
            ILogger logger,
            IHotkeyInterceptionFactory hotkeyInterceptionFactory)
        {
            this.logger = logger;

            hotkeyInterception = hotkeyInterceptionFactory.CreateInterception(
                KeyboardApi.VK_KEY_V,
                true,
                true);
        }

        public void Install(IntPtr windowHandle)
        {
            if (isInstalled)
            {
                throw new InvalidOperationException("This interceptor has already been installed.");
            }

            this.mainWindowHandle = windowHandle;
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

            //TODO: proper exceptions for uninstall for ALL uninstalls (don't uninstall when already uninstalled etc).
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
            logger.Information("Paste hotkey message received.", 1);

            HotkeyFired?.Invoke(
                this,
                new HotkeyFiredArgument(
                    hotkeyInterception.KeyCode,
                    hotkeyInterception.ControlNeeded));
        }
    }
}