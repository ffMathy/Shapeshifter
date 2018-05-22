namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Diagnostics;

	using Infrastructure.Caching.Interfaces;

	using Interfaces;

	using Native;
    using Native.Interfaces;

    public class WindowManager: IWindowManager
    {
        readonly IWindowNativeApi windowNativeApi;
		readonly IKeyValueCache<IntPtr, Process> dataSourceProcessCache;

		readonly WindowNativeApi.WinEventDelegate callbackPointer;

        IntPtr hookHandle;
        
        public event EventHandler ActiveWindowChanged;
        public event EventHandler ActiveWindowProcessChanged;

        public bool IsConnected { get; private set; }

        public IntPtr ActiveWindowHandle { get; private set; }

        public WindowManager(
            IWindowNativeApi windowNativeApi,
			IKeyValueCache<IntPtr, Process> dataSourceProcessCache)
        {
            this.windowNativeApi = windowNativeApi;
			this.dataSourceProcessCache = dataSourceProcessCache;

			this.callbackPointer = OnWindowChanged;
			GC.KeepAlive(callbackPointer);
        }

		public Process GetProcessFromWindowHandle(IntPtr handle)
        {
			var process = dataSourceProcessCache.Get(handle);
			if (process != default) 
				return process;

			windowNativeApi.GetWindowThreadProcessId(handle, out var processId);
			process = Process.GetProcessById((int) processId);
			dataSourceProcessCache.Set(handle, process);

			return process;
		}

        public string GetWindowTitleFromWindowHandle(IntPtr handle)
        {
			var windowTitle = windowNativeApi.GetWindowTitle(handle);
			return windowTitle;
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
                IntPtr.Zero, callbackPointer, 0, 0, 
                WindowNativeApi.WINEVENT_OUTOFCONTEXT);

            IsConnected = true;
        }

        void OnWindowChanged(IntPtr hwineventhook, uint eventtype, IntPtr hwnd, int idobject, int idchild, uint dweventthread, uint dwmseventtime)
		{
			ActiveWindowHandle = hwnd;
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