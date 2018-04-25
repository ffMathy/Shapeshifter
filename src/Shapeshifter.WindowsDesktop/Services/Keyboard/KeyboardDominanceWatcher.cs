namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
	using System.Linq;
	using System.Runtime.Remoting;
	using System.Security.Principal;
	using System.Threading.Tasks;

	using EasyHook;

	using Infrastructure.Events;

	using Interfaces;

	using KeyboardHookInterception;

	using Processes.Interfaces;
	using System;
	using System.Diagnostics.CodeAnalysis;
	using Serilog;
	using Serilog.Core;
	using System.Threading;
	using System.IO;

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
				var embeddedFile = GetInjectedLibraryName();
				var targetResourceName = "costura." + embeddedFile.ToLower();

				logger.Verbose("Looking for {resourceName} among embedded resources {@resources}.", targetResourceName, App.ResourceAssembly.GetManifestResourceNames());

				using (var stream = App.ResourceAssembly.GetManifestResourceStream(targetResourceName)) {
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);

					logger.Verbose("Resource {resourceName} of {length} bytes written to {embeddedFile}.", targetResourceName, bytes.Length, embeddedFile);
					File.WriteAllBytes(embeddedFile, bytes);
				}

				Thread.Sleep(1000);

                Config.Register(
                    nameof(Shapeshifter),
                    $"{processManager.CurrentProcessName}.exe",
					embeddedFile);
					
				logger.Information("Injection mechanism installed and configured in the Global Assembly Cache.");
			} catch(Exception ex)
            {
                logger.Error(ex, "Could not install the keyboard dominance watcher injection mechanism into the Global Assembly Cache.");
            }
        }
    }
}