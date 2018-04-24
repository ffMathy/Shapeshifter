namespace Shapeshifter.WindowsDesktop.Services.Processes
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Security.Principal;

	using Interfaces;
	using Serilog;

	class ProcessManager
		: IProcessManager
	{
		readonly ILogger logger;

		readonly ICollection<Process> processes;
		readonly Process currentProcess;

		public ProcessManager(
			ILogger logger)
		{
			processes = new HashSet<Process>();
			currentProcess = Process.GetCurrentProcess();

			this.logger = logger;
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
			return Path.Combine(
				GetCurrentProcessDirectory(),
				$"{CurrentProcessName}.exe");
		}

		public string GetCurrentProcessDirectory()
		{
			return Environment.CurrentDirectory;
		}

		public void LaunchCommand(string command, string arguments = null)
		{
			SpawnProcess(command, Environment.CurrentDirectory);
		}

		public void CloseAllDuplicateProcessesExceptCurrent()
		{
			logger.Verbose("Closing all duplicate processes.");

			var processes = Process.GetProcessesByName(currentProcess.ProcessName);
			CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
		}

		static void CloseProcessesExceptProcessWithId(
			int processId,
			params Process[] targetProcesses)
		{
			foreach (var process in targetProcesses)
			{
				if (process.Id == processId)
				{
					continue;
				}

				CloseProcess(process);
			}
		}

		static void CloseProcess(Process process)
		{
			try
			{
				if (process.HasExited)
				{
					return;
				}

				if (CloseMainWindow(process))
				{
					return;
				}

				process.Kill();
			}
			catch (Win32Exception)
			{
				//trying to kill an elevated process.
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

		public void LaunchFile(string fileName, string arguments = null)
		{
			var workingDirectory = Path.GetDirectoryName(fileName);
			SpawnProcess(fileName, workingDirectory, arguments);
		}

		public void LaunchFileWithAdministrativeRights(string fileName, string arguments = null)
		{
			var workingDirectory = Path.GetDirectoryName(fileName);
			SpawnProcess(fileName, workingDirectory, arguments, "runas");
		}

		public bool IsCurrentProcessElevated()
		{
			using (var identity = WindowsIdentity.GetCurrent())
			{
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		void SpawnProcess(string uri, string workingDirectory, string arguments = null, string verb = null)
		{
			using (CrossThreadLogContext.Add("fileName", uri))
			using (CrossThreadLogContext.Add("workingDirectory", workingDirectory))
			using (CrossThreadLogContext.Add("verb", verb))
			using (CrossThreadLogContext.Add("arguments", arguments))
			{
				logger.Verbose("Launching {verb} {fileName} in {workingDirectory} with arguments {arguments}.");

				var process = Process.Start(
					new ProcessStartInfo {
						FileName = uri,
						WorkingDirectory = workingDirectory,
						Verb = verb,
						Arguments = arguments
					});
				processes.Add(process);
			}
		}
	}
}