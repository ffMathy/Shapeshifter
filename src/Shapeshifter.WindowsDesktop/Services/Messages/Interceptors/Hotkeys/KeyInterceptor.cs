namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Factories.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    class KeyInterceptor: IKeyInterceptor
    {
        readonly IHotkeyInterceptionFactory hotkeyInterceptionFactory;

        readonly IUserInterfaceThread userInterfaceThread;

        readonly IDictionary<int, IHotkeyInterception> keyInterceptions;

        bool isInstalled;

        IntPtr installedWindowHandle;

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

            installedWindowHandle = windowHandle;

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

        IHotkeyInterception GetInterceptionForInterceptionId(int interceptionId)
        {
            return keyInterceptions.Values
                                   .SingleOrDefault(x => x.InterceptionId == interceptionId);
        }

        void HandleHotkeyMessage(WindowMessageReceivedArgument e)
        {
            if (!isInstalled)
            {
                return;
            }

            var interception = GetInterceptionForInterceptionId((int) e.WordParameter);
            if (interception != null)
            {
                HotkeyFired?.Invoke(
                    this,
                    new HotkeyFiredArgument(
                        interception.KeyCode,
                        interception.ControlNeeded));
            }
        }

        void HandleWindowVisibilityChangedMessage(WindowMessageReceivedArgument e)
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
                throw new InvalidOperationException(
                    "This interceptor has already been uninstalled.");
            }

            foreach (var interception in keyInterceptions.Values)
            {
                interception.Stop(installedWindowHandle);
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

        IHotkeyInterception CreateNewInterception(int keyCode)
        {
            var interception = hotkeyInterceptionFactory.CreateInterception(
                keyCode,
                true,
                false);
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