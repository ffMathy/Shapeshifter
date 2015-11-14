#region

using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    [ExcludeFromCodeCoverage]
    internal class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        private readonly ILogger logger;
        private readonly IHotkeyInterception hotkeyInterception;

        private IntPtr windowHandle;

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
            //TODO: proper exceptions for install for ALL installs (don't install when already installed etc).
            this.windowHandle = windowHandle;
            hotkeyInterception.Start(windowHandle);
        }

        public void Uninstall()
        {
            //TODO: proper exceptions for uninstall for ALL uninstalls (don't uninstall when already uninstalled etc).
            hotkeyInterception.Stop(windowHandle);
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

            if (HotkeyFired != null)
            {
                HotkeyFired(this, new HotkeyFiredArgument(
                    hotkeyInterception.KeyCode, hotkeyInterception.ControlNeeded));
            }
        }
    }
}