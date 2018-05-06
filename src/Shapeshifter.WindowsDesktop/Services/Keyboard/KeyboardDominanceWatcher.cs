namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
	using System.Linq;
	using System.Runtime.Remoting;
	using System.Security.Principal;

	using EasyHook;

	using Infrastructure.Events;

	using Interfaces;

	using KeyboardHookInterception;

	using Processes.Interfaces;

	using Serilog;

	public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
	{
		readonly IProcessWatcher processWatcher;
		readonly ILogger logger;

		readonly HookHostCommunicator communicator;

		static readonly string[] SuspiciousProcesses = {
			"mstsc.exe",
			"teamviewer.exe"
		};

		public KeyboardDominanceWatcher(
			IProcessWatcher processWatcher,
			ILogger logger)
		{
			this.processWatcher = processWatcher;
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
			if (!SuspiciousProcesses.Contains(e.ProcessName)) 
				return;

			logger.Information($"Injecting keyboard override library into process {e.ProcessName}.");

			var injectedLibraryName = GetInjectedLibraryName();

			string channelName = null;
			RemoteHooking.IpcCreateServer(
				ref channelName,
				WellKnownObjectMode.SingleCall,
				communicator,
				WellKnownSidType.WorldSid);

			const InjectionOptions injectionOptions =
				InjectionOptions.DoNotRequireStrongName &
				InjectionOptions.NoService &
				InjectionOptions.NoWOW64Bypass;

			RemoteHooking.Inject(
				e.ProcessId,
				injectionOptions,
				injectedLibraryName,
				injectedLibraryName,
				channelName);

			logger.Information($"Keyboard override library successfully injected.");
		}

		public void Start()
		{
			processWatcher.Connect();
		}

		public void Stop()
		{
			processWatcher.Disconnect();
		}

		static string GetInjectedLibraryName()
		{
			return $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(KeyboardHookInterception)}.dll";
		}
	}
}
