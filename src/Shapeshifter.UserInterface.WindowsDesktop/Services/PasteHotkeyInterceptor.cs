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
        private readonly IWindowMessageHook windowMessageHook;

        public event EventHandler<PasteHotkeyFiredArgument> PasteHotkeyFired;

        public bool IsConnected
        {
            get;private set;
        }

        public PasteHotkeyInterceptor(
            IWindowMessageHook windowMessageHook)
        {
            this.windowMessageHook = windowMessageHook;
        }

        public void Connect()
        {
            EnsureWindowIsPresent();

            if (!IsConnected)
            {
                InstallWindowHook();
                InstallHotkeyHook();
                IsConnected = true;
            }
        }

        private void InstallHotkeyHook()
        {
            KeyboardApi.RegisterHotKey(windowMessageHook.MainWindowHandle, GetHashCode(), KeyboardApi.MOD_CONTROL, (int)Key.V);
        }

        private void InstallWindowHook()
        {
            windowMessageHook.MessageReceived += WindowMessageHook_MessageReceived;
            windowMessageHook.Connect();
        }

        private void WindowMessageHook_MessageReceived(object sender, WindowMessageReceivedArgument e)
        {
            throw new NotImplementedException();
        }

        private static void EnsureWindowIsPresent()
        {
            var mainWindow = App.Current.MainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Can't install a clipboard hook when there is no window open.");
            }
        }

        public void Disconnect()
        {
            if(IsConnected)
            {
                UninstallHotkeyHook();
                UninstallWindowHook();

                IsConnected = false;
            }
        }

        private void UninstallWindowHook()
        {
            //can't disconnect the windowMessageHook here as it might be used somewhere else. what else can we do?
            windowMessageHook.MessageReceived -= WindowMessageHook_MessageReceived;
        }

        private void UninstallHotkeyHook()
        {
            KeyboardApi.UnregisterHotKey(windowMessageHook.MainWindowHandle, GetHashCode());
        }

        public void SendPasteCombination()
        {
            throw new NotImplementedException();
        }
    }
}
