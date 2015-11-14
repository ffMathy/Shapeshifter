using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    [ExcludeFromCodeCoverage]
    internal class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        private readonly ILogger logger;
        private readonly IHotkeyInterception hotkeyInterception;

        private IntPtr mainWindowHandle;

        private bool isInstalled;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

        public PasteHotkeyInterceptor(
            ILogger logger,
            IHotkeyInterceptionFactory hotkeyInterceptionFactory)
        {
            this.logger = logger;

            hotkeyInterception = hotkeyInterceptionFactory.CreateInterception(
                KeyboardApi.VK_KEY_V, true, true);
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
                throw new InvalidOperationException("This interceptor has already been uninstalled.");
            }

            //TODO: proper exceptions for uninstall for ALL uninstalls (don't uninstall when already uninstalled etc).
            hotkeyInterception.Stop(mainWindowHandle);

            isInstalled = false;
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if (e.Message == Message.WM_HOTKEY && (int) e.WordParameter == hotkeyInterception.InterceptionId)
            {
                HandleHotkeyMessage();
            }
        }

        private void HandleHotkeyMessage()
        {
            logger.Information("Paste hotkey message received.", 1);

            HotkeyFired?.Invoke(this, new HotkeyFiredArgument(
                hotkeyInterception.KeyCode, hotkeyInterception.ControlNeeded));
        }
    }
}