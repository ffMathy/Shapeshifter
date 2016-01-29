namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Diagnostics;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    public class WindowManager: IWindowManager
    {
        readonly IWindowNativeApi windowNativeApi;

        IntPtr hookHandle;
        
        public event EventHandler ActiveWindowChanged;
        public event EventHandler ActiveWindowProcessChanged;

        public bool IsConnected { get; private set; }

        public WindowManager(
            IWindowNativeApi windowNativeApi)
        {
            this.windowNativeApi = windowNativeApi;
        }

        public Process GetActiveWindowProcess()
        {
            throw new NotImplementedException();
        }

        public string GetActiveWindowProcessTitle()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Can't disconnect the hook twice.");
            }

            if (!windowNativeApi.UnhookWinEvent(hookHandle))
            {
                throw new InvalidOperationException("Could not unhook window hook.");
            }

            IsConnected = false;
        }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Can't connect the hook twice.");
            }

            hookHandle = windowNativeApi.SetWinEventHook(
                WindowNativeApi.EVENT_SYSTEM_FOREGROUND,
                WindowNativeApi.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, OnWindowChanged, 0, 0, 
                WindowNativeApi.WINEVENT_OUTOFCONTEXT);

            IsConnected = true;
        }

        void OnWindowChanged(IntPtr hwineventhook, uint eventtype, IntPtr hwnd, int idobject, int idchild, uint dweventthread, uint dwmseventtime)
        {
            OnActiveWindowChanged();
        }

        protected virtual void OnActiveWindowChanged()
        {
            ActiveWindowChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnActiveWindowProcessChanged()
        {
            ActiveWindowProcessChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}