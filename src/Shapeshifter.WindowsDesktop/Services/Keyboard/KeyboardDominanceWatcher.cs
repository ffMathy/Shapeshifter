namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Remoting;
    using System.Threading;

    using EasyHook;

    using Infrastructure.Events;

    using Interfaces;

    using KeyboardHookInterception;

    using Processes.Interfaces;

    using Services.Interfaces;

    public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
    {
        readonly IProcessWatcher processWatcher;
        readonly IProcessManager processManager;

        readonly HookHostCommunicator communicator;

        string channelName;

        static readonly string[] SuspiciousProcesses = new[]
        {
            "mstsc.exe",
            "teamviewer.exe"
        };

        public KeyboardDominanceWatcher(
            IProcessWatcher processWatcher,
            IProcessManager processManager)
        {
            this.processWatcher = processWatcher;
            this.processManager = processManager;

            communicator = new HookHostCommunicator();

            SetUpProcessWatcher();
        }

        void SetUpProcessWatcher()
        {
            processWatcher.ProcessStarted += ProcessWatcher_ProcessStarted;
            foreach (var process in SuspiciousProcesses)
            {
                processWatcher.AddProcessNameToWatchList(process);
            }
        }

        void ProcessWatcher_ProcessStarted(object sender, ProcessStartedEventArgument e)
        {
            if (!SuspiciousProcesses.Contains(e.ProcessName)) return;

            var injectedLibraryName = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(KeyboardHookInterception)}.dll";
            Config.Register(
                nameof(Shapeshifter),
                $"{processManager.GetCurrentProcessName()}.exe",
                injectedLibraryName);

            RemoteHooking.IpcCreateServer(
                ref channelName, 
                WellKnownObjectMode.SingleCall,
                communicator);
            
            RemoteHooking.Inject(
                e.ProcessId,
                injectedLibraryName,
                injectedLibraryName,
                channelName);
        }

        public void Start()
        {
            processWatcher.Connect();
        }

        public void Stop()
        {
            processWatcher.Disconnect();
        }
    }
}