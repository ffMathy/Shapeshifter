using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    class KeyInterceptor : IKeyInterceptor
    {
        readonly IHotkeyInterceptionFactory hotkeyInterceptionFactory;

        IDictionary<int, IHotkeyInterception> keyInterceptions;

        bool isInstalled;

        IntPtr windowHandle;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

        public KeyInterceptor(
            IHotkeyInterceptionFactory hotkeyInterceptionFactory)
        {
            this.hotkeyInterceptionFactory = hotkeyInterceptionFactory;

            keyInterceptions = new Dictionary<int, IHotkeyInterception>();
        }

        public void Install(IntPtr windowHandle)
        {
            this.windowHandle = windowHandle;

            foreach(var interception in keyInterceptions.Values)
            {
                interception.Start(windowHandle);
            }

            isInstalled = true;
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            switch(e.Message)
            {
                case WindowApi.WM_SHOWWINDOW:
                    HandleWindowVisibilityChangedMessage(e);
                    break;

                case KeyboardApi.WM_HOTKEY:
                    HandleHotkeyMessage(e);
                    break;
            }
        }

        IHotkeyInterception GetInterceptionForInterceptionId(int interceptionId)
        {
            return keyInterceptions.Values
                .SingleOrDefault(x => x.InterceptionId == interceptionId);
        }

        void HandleHotkeyMessage(WindowMessageReceivedArgument e)
        {
            var interception = GetInterceptionForInterceptionId((int)e.WordParameter);
            if(interception != null && HotkeyFired != null)
            {
                HotkeyFired(this, new HotkeyFiredArgument(
                    interception.KeyCode, interception.ControlNeeded));
            }
        }

        void HandleWindowVisibilityChangedMessage(WindowMessageReceivedArgument e)
        {
            const int Shown = 1;
            const int Hidden = 0;

            switch ((int)e.WordParameter)
            {
                case Shown:
                    Install(e.WindowHandle);
                    break;

                case Hidden:
                    Uninstall();
                    break;
            }
        }

        public void Uninstall()
        {
            foreach (var interception in keyInterceptions.Values)
            {
                interception.Stop(windowHandle);
            }

            isInstalled = false;
        }

        public void AddInterceptingKey(
            IntPtr windowHandle, 
            int keyCode)
        {
            if (keyInterceptions.ContainsKey(keyCode))
            {
                return;
            }

            var interception = CreateNewInterception(keyCode);
            if(isInstalled)
            {
                interception.Start(windowHandle);
            }

            keyInterceptions.Add(keyCode, interception);
        }

        IHotkeyInterception CreateNewInterception(int keyCode)
        {
            var interception = hotkeyInterceptionFactory.CreateInterception(
                            keyCode, true, false);
            return interception;
        }

        public void RemoveInterceptingKey(
            IntPtr windowHandle,
            int keyCode)
        {
            if(!keyInterceptions.ContainsKey(keyCode))
            {
                return;
            }

            var interception = keyInterceptions[keyCode];
            if (isInstalled)
            {
                interception.Stop(windowHandle);
            }

            keyInterceptions.Remove(keyCode);
        }
    }
}
