namespace Shapeshifter.WindowsDesktop.Services.Processes.Interfaces
{
	using System;
	using System.Diagnostics;

	public interface IProcessManager : IDisposable
	{
		Process LaunchFile(string fileName, string arguments = null);
		Process LaunchFileWithAdministrativeRights(string fileName, string arguments = null);

		bool IsCurrentProcessElevated();

		string GetCurrentProcessFilePath();
		string GetCurrentProcessDirectory();

		string CurrentProcessName { get; }
		int CurrentProcessId { get; }

		Process LaunchCommand(string command, string arguments = null);

		void CloseAllDuplicateProcessesExceptCurrent();
		void CloseCurrentProcess();
	}
}