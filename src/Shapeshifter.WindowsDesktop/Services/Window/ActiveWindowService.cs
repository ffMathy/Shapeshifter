namespace Shapeshifter.WindowsDesktop.Services.Window
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

	using Information;

    using Infrastructure.Caching.Interfaces;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    using Processes.Interfaces;

    public class ActiveWindowService : IActiveWindowService
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
			const string applicationFrameHostName = "ApplicationFrameHost";

            var process = windowProcessCache.Get(handle);
            if (process != default && process.ProcessName != applicationFrameHostName)
                return process;

            windowNativeApi.GetWindowThreadProcessId(handle, out var processId);
            if (processId == CurrentProcessInformation.CurrentProcess.Id)
                return CurrentProcessInformation.CurrentProcess;

            process = Process.GetProcessById((int)processId);
			if (process.ProcessName == applicationFrameHostName)
                process = GetProcessFromUniversalWindowsApplication(handle, process);

            windowProcessCache.Set(handle, process);

            return process;
        }

        Process GetProcessFromUniversalWindowsApplication(IntPtr windowHandle, Process process)
        {
            var windowinfo = new WindowNativeApi.WINDOWINFO
            {
                ownerpid = (uint)process.Id,
                childpid = (uint)process.Id
            };

            var pWindowinfo = Marshal.AllocHGlobal(Marshal.SizeOf(windowinfo));
			try
			{
				Marshal.StructureToPtr(windowinfo, pWindowinfo, false);

				var lpEnumFunc = new WindowNativeApi.EnumWindowProc(EnumChildWindowsCallback);
				windowNativeApi.EnumChildWindows(windowHandle, lpEnumFunc, pWindowinfo);

				windowinfo = (WindowNativeApi.WINDOWINFO) Marshal.PtrToStructure(pWindowinfo, typeof(WindowNativeApi.WINDOWINFO));

				return Process.GetProcessById((int) windowinfo.childpid);
			}
			finally
			{
				Marshal.FreeHGlobal(pWindowinfo);
			}
        }

		bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
		{
			var info = (WindowNativeApi.WINDOWINFO)Marshal.PtrToStructure(lParam, typeof(WindowNativeApi.WINDOWINFO));

			windowNativeApi.GetWindowThreadProcessId(hWnd, out var processId);

            var process = Process.GetProcessById((int)processId);
			if (process.Id != info.ownerpid)
				info.childpid = (uint)process.Id;

			Marshal.StructureToPtr(info, lParam, true);

			return true;
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
            var newProcess = GetProcessFromWindowHandle(hwnd);
            if (newProcess == CurrentProcessInformation.CurrentProcess)
                return;

            var oldProcess = GetProcessFromWindowHandle(ActiveWindowHandle);
            var previousThreadId = GetProcessActiveWindowUserInterfaceThreadId(oldProcess);
            if (previousThreadId != null)
                windowThreadMerger.UnmergeThread(previousThreadId.Value);

            ActiveWindowHandle = hwnd;

            var currentThreadId = GetProcessActiveWindowUserInterfaceThreadId(newProcess);
            if (currentThreadId != null)
                windowThreadMerger.MergeThread(currentThreadId.Value);

            OnActiveWindowChanged();
        }

        int? GetProcessActiveWindowUserInterfaceThreadId(Process process)
        {
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