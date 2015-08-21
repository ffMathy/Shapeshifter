using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard
{
    [ExcludeFromCodeCoverage]
    class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        public event EventHandler<PasteHotkeyFiredArgument> PasteHotkeyFired;

        public void Install(IntPtr windowHandle)
        {
            KeyboardApi.RegisterHotKey(windowHandle, GetHashCode(), KeyboardApi.MOD_CONTROL | KeyboardApi.MOD_NOREPEAT, (int)Key.V);
        }

        public void Uninstall(IntPtr windowHandle)
        {
            KeyboardApi.UnregisterHotKey(windowHandle, GetHashCode());
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if (e.Message == KeyboardApi.WM_HOTKEY && PasteHotkeyFired != null)
            {
                PasteHotkeyFired(this, new PasteHotkeyFiredArgument());
            }
        }
    }
}
