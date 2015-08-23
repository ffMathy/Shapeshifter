using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard
{
    [ExcludeFromCodeCoverage]
    class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        readonly ILogger logger;
        readonly IEnvironmentInformation environmentInformation;

        public event EventHandler<PasteHotkeyFiredArgument> PasteHotkeyFired;

        public PasteHotkeyInterceptor(
            ILogger logger,
            IEnvironmentInformation environmentInformation)
        {
            this.logger = logger;
            this.environmentInformation = environmentInformation;
        }

        public void Install(IntPtr windowHandle)
        {
            UnregisterHotkey(windowHandle);
            if(!RegisterHotkey(windowHandle))
            {
                throw new InvalidOperationException("Couldn't install the paste hotkey interceptor.");
            }
        }

        bool RegisterHotkey(IntPtr windowHandle)
        {
            return KeyboardApi.RegisterHotKey(windowHandle, GetInterceptorId(), KeyboardApi.MOD_CONTROL, KeyboardApi.VK_KEY_V);
        }

        int GetInterceptorId()
        {
            if (environmentInformation.IsDebugging)
            {
                return 1337;
            }
            else
            {
                return GetHashCode();
            }
        }

        public void Uninstall(IntPtr windowHandle)
        {
            if(!UnregisterHotkey(windowHandle))
            {
                throw new InvalidOperationException("Couldn't uninstall the paste hotkey interceptor.");
            }
        }

        bool UnregisterHotkey(IntPtr windowHandle)
        {
            return KeyboardApi.UnregisterHotKey(windowHandle, GetInterceptorId());
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if (e.Message == KeyboardApi.WM_HOTKEY)
            {
                logger.Information("Hotkey message received.");

                if (PasteHotkeyFired != null)
                {
                    PasteHotkeyFired(this, new PasteHotkeyFiredArgument());
                }
            }
        }
    }
}
