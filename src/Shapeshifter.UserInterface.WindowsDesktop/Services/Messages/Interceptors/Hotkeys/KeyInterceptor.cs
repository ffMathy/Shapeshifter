﻿using System;
using System.Collections.Generic;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    internal class KeyInterceptor : IKeyInterceptor
    {
        private readonly IHotkeyInterceptionFactory hotkeyInterceptionFactory;
        private readonly IUserInterfaceThread userInterfaceThread;

        private readonly IDictionary<int, IHotkeyInterception> keyInterceptions;

        private bool isInstalled;

        private IntPtr mainWindowHandle;

        public event EventHandler<HotkeyFiredArgument> HotkeyFired;

        public KeyInterceptor(
            IHotkeyInterceptionFactory hotkeyInterceptionFactory,
            IUserInterfaceThread userInterfaceThread)
        {
            this.hotkeyInterceptionFactory = hotkeyInterceptionFactory;
            this.userInterfaceThread = userInterfaceThread;

            keyInterceptions = new Dictionary<int, IHotkeyInterception>();
        }

        public void Install(IntPtr windowHandle)
        {
            if (isInstalled)
            {
                throw new InvalidOperationException("This interceptor has already been installed.");
            }

            this.mainWindowHandle = windowHandle;

            foreach (var interception in keyInterceptions.Values)
            {
                interception.Start(windowHandle);
            }

            isInstalled = true;
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            switch (e.Message)
            {
                case Message.WM_SHOWWINDOW:
                    userInterfaceThread.Invoke(() => HandleWindowVisibilityChangedMessage(e));
                    break;

                case Message.WM_HOTKEY:
                    HandleHotkeyMessage(e);
                    break;
            }
        }

        private IHotkeyInterception GetInterceptionForInterceptionId(int interceptionId)
        {
            return keyInterceptions.Values
                .SingleOrDefault(x => x.InterceptionId == interceptionId);
        }

        private void HandleHotkeyMessage(WindowMessageReceivedArgument e)
        {
            var interception = GetInterceptionForInterceptionId((int) e.WordParameter);
            if (interception != null)
            {
                HotkeyFired?.Invoke(this, new HotkeyFiredArgument(
                    interception.KeyCode, interception.ControlNeeded));
            }
        }

        private void HandleWindowVisibilityChangedMessage(WindowMessageReceivedArgument e)
        {
            const int Shown = 1;
            const int Hidden = 0;

            switch ((int) e.WordParameter)
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
            if (!isInstalled)
            {
                throw new InvalidOperationException("This interceptor has already been uninstalled.");
            }

            foreach (var interception in keyInterceptions.Values)
            {
                interception.Stop(mainWindowHandle);
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
            if (isInstalled)
            {
                interception.Start(windowHandle);
            }

            keyInterceptions.Add(keyCode, interception);
        }

        private IHotkeyInterception CreateNewInterception(int keyCode)
        {
            var interception = hotkeyInterceptionFactory.CreateInterception(
                keyCode, true, false);
            return interception;
        }

        public void RemoveInterceptingKey(
            IntPtr windowHandle,
            int keyCode)
        {
            if (!keyInterceptions.ContainsKey(keyCode))
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