namespace Shapeshifter.WindowsDesktop.Services.Window
{
	using System;
	using System.Diagnostics;

	using Information;

	using Infrastructure.Caching.Interfaces;

	using Interfaces;

	using Native;
	using Native.Interfaces;

	using Processes.Interfaces;

	public class ActiveWindowService: IActiveWindowService
    {
        readonly IWindowNativeApi windowNativeApi;
		readonly IWindowThreadMerger windowThreadMerger;
		readonly IProcessManager processManager;

		readonly IKeyValueCache<IntPtr, Process> windowProcessCache;

		readonly WindowNativeApi.WinEventDelegate callbackPointer;

        IntPtr hookHandle;
        
        public event EventHandler ActiveWindowChanged;
        public event EventHandler ActiveWindowProcessChanged;

        public bool IsConnected { get; private set; }

        public IntPtr ActiveWindowHandle { get; private set; }

        public ActiveWindowService(
            IWindowNativeApi windowNativeApi,
			IWindowThreadMerger windowThreadMerger,
			IProcessManager processManager,
			IKeyValueCache<IntPtr, Process> windowProcessCache)
        {
            this.windowNativeApi = windowNativeApi;
			this.windowThreadMerger = windowThreadMerger;
			this.processManager = processManager;
			this.windowProcessCache = windowProcessCache;

			callbackPointer = OnWindowChanged;
			GC.KeepAlive(callbackPointer);
        }

		public Process GetProcessFromWindowHandle(IntPtr handle)
        {
			var process = windowProcessCache.Get(handle);
			if (process != default) 
				return process;

			windowNativeApi.GetWindowThreadProcessId(handle, out var processId);
			process = Process.GetProcessById((int) processId);
			windowProcessCache.Set(handle, process);

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
                throw new InvalidOperationException("Can't disconnect the hook twice.");

            if (!windowNativeApi.UnhookWinEvent(hookHandle))
                throw new InvalidOperationException("Could not unhook window hook.");

            IsConnected = false;
        }

        public void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException("Can't connect the hook twice.");

            hookHandle = windowNativeApi.SetWinEventHook(
                WindowNativeApi.EVENT_SYSTEM_FOREGROUND,
                WindowNativeApi.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, callbackPointer, 0, 0, 
                WindowNativeApi.WINEVENT_OUTOFCONTEXT);

            IsConnected = true;
        }

        void OnWindowChanged(IntPtr hwineventhook, uint eventtype, IntPtr hwnd, int idobject, int idchild, uint dweventthread, uint dwmseventtime)
		{
			var previousThreadId = GetActiveWindowUserInterfaceThreadId();
			if(previousThreadId != null)
				windowThreadMerger.UnmergeThread(previousThreadId.Value);

            ActiveWindowHandle = hwnd;

			var currentThreadId = GetActiveWindowUserInterfaceThreadId();
			if (currentThreadId != null)
                windowThreadMerger.MergeThread(currentThreadId.Value);

            OnActiveWindowChanged();
        }

        int? GetActiveWindowUserInterfaceThreadId()
		{
			var process = GetProcessFromWindowHandle(ActiveWindowHandle);
			if (process.Id == CurrentProcessInformation.CurrentProcess.Id)
				return null;

            var thread = processManager.GetUserInterfaceThreadOfProcess(process);
			return thread?.Id;
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