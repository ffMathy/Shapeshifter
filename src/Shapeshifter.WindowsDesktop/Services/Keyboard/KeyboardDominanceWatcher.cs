namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Linq;
    using System.Runtime.Remoting;
    using System.Security.Principal;
    using System.Threading.Tasks;

    using EasyHook;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using KeyboardHookInterception;

    using Processes.Interfaces;
    using System;
    using System.Diagnostics.CodeAnalysis;

    public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
    {
        readonly IProcessWatcher processWatcher;
        readonly IProcessManager processManager;
        readonly ILogger logger;

        readonly HookHostCommunicator communicator;

        static readonly string[] SuspiciousProcesses = new[]
        {
            "mstsc.exe",
            "teamviewer.exe"
        };

        public KeyboardDominanceWatcher(
            IProcessWatcher processWatcher,
            IProcessManager processManager,
            ILogger logger)
        {
            this.processWatcher = processWatcher;
            this.processManager = processManager;
            this.logger = logger;

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

        async void ProcessWatcher_ProcessStarted(object sender, ProcessStartedEventArgument e)
        {
            if (!SuspiciousProcesses.Contains(e.ProcessName)) return;

            logger.Information($"Injecting keyboard override library into process {e.ProcessName}.");

            var injectedLibraryName = GetInjectedLibraryName();
            
            string channelName = null;
            RemoteHooking.IpcCreateServer(
                ref channelName, 
                WellKnownObjectMode.SingleCall,
                communicator,
                WellKnownSidType.WorldSid);

            await Task.Delay(1000);
            
            RemoteHooking.Inject(
                e.ProcessId,
                injectedLibraryName,
                injectedLibraryName,
                channelName);

            logger.Information($"Keyboard override library successfully injected.");
        }

        static string GetInjectedLibraryName()
        {
            return $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(KeyboardHookInterception)}.dll";
        }

        public void Start()
        {
            processWatcher.Connect();
        }

        public void Stop()
        {
            processWatcher.Disconnect();
        }

        [ExcludeFromCodeCoverage]
        public void Install()
        {
            try
            {
                Config.Register(
                    nameof(Shapeshifter),
                    $"{processManager.GetCurrentProcessName()}.exe",
                    GetInjectedLibraryName());
            } catch(Exception ex)
            {
                logger.Error(
                    new Exception(
                        "Could not install the keyboard dominance watcher injection mechanism into the Global Assembly Cache.", 
                        ex));
            }
        }
    }
}