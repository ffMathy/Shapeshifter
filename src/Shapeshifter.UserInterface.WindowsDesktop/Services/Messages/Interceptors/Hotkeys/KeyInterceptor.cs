using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    class KeyInterceptor : IKeyInterceptor
    {
        readonly IWindowMessageHook windowMessageHook;
        readonly IHotkeyInterceptionFactory hotkeyInterceptionFactory;

        IDictionary<int, IHotkeyInterception> keyInterceptions;

        public bool IsManagedAutomatically => false;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

        public KeyInterceptor(
            IWindowMessageHook windowMessageHook,
            IHotkeyInterceptionFactory hotkeyInterceptionFactory)
        {
            this.windowMessageHook = windowMessageHook;
            this.hotkeyInterceptionFactory = hotkeyInterceptionFactory;

            keyInterceptions = new Dictionary<int, IHotkeyInterception>();
        }

        public void Install(IntPtr windowHandle)
        {
            throw new NotImplementedException();
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            throw new NotImplementedException();
        }

        public void Uninstall(IntPtr windowHandle)
        {
            throw new NotImplementedException();
        }

        public void StartInterceptingKey(int keyCode)
        {
            if (keyInterceptions.ContainsKey(keyCode))
            {
                return;
            }

            var interception = CreateNewInterception(keyCode);
            interception.Start(windowMessageHook.MainWindowHandle);

            keyInterceptions.Add(keyCode, interception);
        }

        IHotkeyInterception CreateNewInterception(int keyCode)
        {
            var interception = hotkeyInterceptionFactory.CreateInterception(
                            keyCode, false, false);
            return interception;
        }

        public void StopInterceptingKey(int keyCode)
        {
            if(!keyInterceptions.ContainsKey(keyCode))
            {
                return;
            }

            var interception = keyInterceptions[keyCode];
            interception.Stop(windowMessageHook.MainWindowHandle);

            keyInterceptions.Remove(keyCode);
        }
    }
}
