namespace Shapeshifter.WindowsDesktop.Services.Processes.Interfaces
{
	using System;
	using System.Diagnostics;

	public interface IProcessManager : IDisposable
	{
		Process LaunchFile(string fileName, string arguments = null, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal);
		Process LaunchFileWithAdministrativeRights(string fileName, string arguments = null, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal);

		bool IsCurrentProcessElevated();

		string GetCurrentProcessFilePath();
		string GetCurrentProcessDirectory();

		string CurrentProcessName { get; }
		int CurrentProcessId { get; }

		ProcessThread GetUserInterfaceThreadOfProcess(Process process);

		Process LaunchCommand(string command, string arguments = null);

		void CloseAllDuplicateProcessesExceptCurrent();
		void CloseCurrentProcess();
	}
}