namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
	using System.Linq;
	using System.Runtime.Remoting;
	using System.Security.Principal;
	using System.Threading.Tasks;
	using System.Collections.Generic;

	using EasyHook;

	using Infrastructure.Events;

	using Interfaces;

	using KeyboardHookInterception;

	using Processes.Interfaces;
	using System;
	using System.Diagnostics.CodeAnalysis;
	using Serilog;

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

		[ExcludeFromCodeCoverage]
		public void Install()
		{
			try
			{
				var processorArchitecture = Environment.Is64BitOperatingSystem ? "64" : "32";

				var dependencyPrefix = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.";

				var dependenciesToSave = new List<string>
				{
					dependencyPrefix + $"EasyHook{processorArchitecture}Svc.exe",
					dependencyPrefix + $"EasyHook{processorArchitecture}.dll",
					dependencyPrefix + $"EasyLoad{processorArchitecture}.dll"
				};

				foreach(var dependency in dependenciesToSave) {
					EmitEmbeddedResourceToDisk(
						dependency, 
						dependency.Substring(
							dependencyPrefix.Length));
				}

				var injectedKeyboardHookInterceptionLibraryName = GetInjectedLibraryName();
				EmitEmbeddedResourceToDisk("costura." + injectedKeyboardHookInterceptionLibraryName.ToLower(), injectedKeyboardHookInterceptionLibraryName);

				var injectedNativeLibraryName = $"{nameof(Shapeshifter)}.{nameof(WindowsDesktop)}.{nameof(Native)}.dll";
				EmitEmbeddedResourceToDisk("costura." + injectedNativeLibraryName.ToLower(), injectedNativeLibraryName);

				logger.Information("Injection mechanism installed and configured in the Global Assembly Cache.");
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Could not install the keyboard dominance watcher injection mechanism into the Global Assembly Cache.");
			}
		}

		void EmitEmbeddedResourceToDisk(string targetResourceName, string targetFile)
		{
			logger.Verbose("Attempting to write resource {resourceName} to {embeddedFile}.", targetResourceName, targetFile);
			using (var stream = App.ResourceAssembly.GetManifestResourceStream(targetResourceName))
			{
				var bytes = new byte[stream.Length];
				stream.Read(bytes, 0, bytes.Length);

				logger.Verbose("Resource {resourceName} of {length} bytes written to {embeddedFile}.", targetResourceName, bytes.Length, targetFile);
				File.WriteAllBytes(targetFile, bytes);
			}
		}
	}
}
