using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard
{
    [ExcludeFromCodeCoverage]
    class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        readonly ILogger logger;
        readonly IHotkeyInterception hotkeyInterception;

        const int MinimumHotkeyIntervalInMilliseconds = 1000;
        DateTime lastHotkeyTime;

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
            hotkeyInterception.Start(windowHandle);
        }

        public void Uninstall(IntPtr windowHandle)
        {
            hotkeyInterception.Stop(windowHandle);
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if (e.Message == KeyboardApi.WM_HOTKEY && (int)e.WordParameter == hotkeyInterception.InterceptionId)
            {
                OnHotkeyMessageReceived();
            }
        }

        void OnHotkeyMessageReceived()
        {
            if (DidHotkeyOccurOutsideInterval())
            {
                HandleHotkeyMessage();
            }
            else
            {
                logger.Information("Paste hotkey message skipped. It was within the hotkey interval.", 1);
            }
        }

        void HandleHotkeyMessage()
        {
            lastHotkeyTime = DateTime.UtcNow;

            logger.Information("Paste hotkey message received.", 1);

            if (HotkeyFired != null)
            {
                HotkeyFired(this, new HotkeyFiredArgument(
                    hotkeyInterception.KeyCode, hotkeyInterception.ControlNeeded));
            }
        }

        bool DidHotkeyOccurOutsideInterval()
        {
            return lastHotkeyTime == default(DateTime) || (DateTime.UtcNow - lastHotkeyTime).TotalMilliseconds > MinimumHotkeyIntervalInMilliseconds;
        }
    }
}
