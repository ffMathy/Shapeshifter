namespace Shapeshifter.WindowsDesktop.Services.Processes
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Security.Principal;
	using System.Threading;
	using Interfaces;

	using Native.Interfaces;

	using Serilog;
	using Serilog.Context;

	class ProcessManager
		: IProcessManager
	{
		readonly ILogger logger;
		readonly IWindowNativeApi windowNativeApi;

		readonly ICollection<Process> processes;
		readonly Process currentProcess;

		public ProcessManager(
			ILogger logger,
			IWindowNativeApi windowNativeApi)
		{
			processes = new HashSet<Process>();
			currentProcess = Process.GetCurrentProcess();

			this.logger = logger;
			this.windowNativeApi = windowNativeApi;
		}

		public void Dispose()
		{
			foreach (var process in processes)
			{
				CloseProcess(process);
			}
		}

		public int CurrentProcessId => currentProcess.Id;

		public string CurrentProcessName => $"{currentProcess.ProcessName}";

		public string GetCurrentProcessFilePath()
		{
			return currentProcess.MainModule.FileName;
		}

		public string GetCurrentProcessDirectory()
		{
			return Path.GetDirectoryName(GetCurrentProcessFilePath());
		}

		public Process LaunchCommand(string command, string arguments = null)
		{
			return SpawnProcess(command, Environment.CurrentDirectory);
		}

		public ProcessThread GetUserInterfaceThreadOfProcess(Process process)
		{
			if (process.MainWindowHandle == IntPtr.Zero)
				return null;

			var processThreadId = windowNativeApi.GetWindowThreadProcessId(
				process.MainWindowHandle,
				out _);
			foreach (ProcessThread processThread in process.Threads)
			{
				if (processThread.Id == processThreadId)
					return processThread;
			}

			return null;
		}

		public void CloseAllDuplicateProcessesExceptCurrent()
		{
			logger.Verbose("Closing all duplicate processes.");

			var currentProcessProductName = GetProcessProductName(currentProcess);
			if (currentProcessProductName == null)
				throw new Exception("Could not detect the current process product name.");

			foreach (var process in Process.GetProcesses())
			{
				try
				{
					if (GetProcessProductName(process) != currentProcessProductName)
						continue;

					if (process.Id == currentProcess.Id)
						continue;

					CloseProcess(process);
				}
				catch (Win32Exception)
				{
					//access denied due to non-elevated process trying to kill elevated one.
				}
				finally
				{
					process.Dispose();
				}
			}

			Thread.Sleep(1000);
		}

		static string GetProcessProductName(Process process)
		{
			try
			{
				return process.MainModule.FileVersionInfo.ProductName;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (InvalidOperationException)
			{
				//process already closed.
				return null;
			}
		}

		void CloseProcess(Process process)
		{
			try
			{
				if (process.HasExited)
					return;

				if (CloseMainWindow(process))
					return;

				logger.Information("Killing process #{processId}.", process.Id);
				process.Kill();
			}
			catch (InvalidOperationException)
			{
				//process already closed.
			}
			finally
			{
				process.Dispose();
			}
		}

		static bool CloseMainWindow(Process process)
		{
			process.CloseMainWindow();
			if (process.WaitForExit(3000))
			{
				return true;
			}
			return false;
		}

		public Process LaunchFile(string fileName, string arguments = null, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
		{
			var workingDirectory = Path.GetDirectoryName(fileName);
			return SpawnProcess(fileName, workingDirectory, arguments, null, windowStyle);
		}

		public Process LaunchFileWithAdministrativeRights(string fileName, string arguments = null, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
		{
			var workingDirectory = Path.GetDirectoryName(fileName);
			return SpawnProcess(fileName, workingDirectory, arguments, "runas", windowStyle);
		}

		public bool IsCurrentProcessElevated()
		{
			using (var identity = WindowsIdentity.GetCurrent())
			{
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		Process SpawnProcess(
			string uri,
			string workingDirectory,
			string arguments = null,
			string verb = null,
			ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
		{
			using (LogContext.PushProperty("fileName", uri))
			using (LogContext.PushProperty("workingDirectory", workingDirectory))
			using (LogContext.PushProperty("verb", verb))
			using (LogContext.PushProperty("arguments", arguments))
			{
				logger.Verbose("Launching {fileName} under verb {verb} in {workingDirectory} with arguments {arguments}.");

				var hasVerb = !string.IsNullOrEmpty(verb);

				var process = Process.Start(
					new ProcessStartInfo {
						FileName = uri,
						WorkingDirectory = workingDirectory,
						Verb = verb,
						Arguments = arguments,
						WindowStyle = windowStyle,
						RedirectStandardError = !hasVerb,
						RedirectStandardInput = !hasVerb,
						RedirectStandardOutput = !hasVerb,
						UseShellExecute = hasVerb,
						CreateNoWindow = windowStyle == ProcessWindowStyle.Hidden && !hasVerb
					});
				processes.Add(process);

				return process;
			}
		}

		public void CloseCurrentProcess()
		{
			CloseProcess(Process.GetCurrentProcess());
		}
	}
}
