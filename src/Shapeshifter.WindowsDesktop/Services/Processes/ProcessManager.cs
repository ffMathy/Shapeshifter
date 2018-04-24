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

			var currentProcessProductName = GetProcessProductName(currentProcess);
			if(currentProcessProductName == null)
				throw new Exception("Could not detect the current process product name.");

			foreach (var process in Process.GetProcesses())
			{
				try
				{
					if (GetProcessProductName(process) != currentProcessProductName)
						continue;

					if (process.Id != currentProcess.Id)
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