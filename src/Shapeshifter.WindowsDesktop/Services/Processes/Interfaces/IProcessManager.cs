namespace Shapeshifter.WindowsDesktop.Services.Processes.Interfaces
{
	using System;

	public interface IProcessManager : IDisposable
	{
		void LaunchFile(string fileName, string arguments = null);
		void LaunchFileWithAdministrativeRights(string fileName, string arguments = null);

		bool IsCurrentProcessElevated();

		string GetCurrentProcessFilePath();
		string GetCurrentProcessDirectory();

		string CurrentProcessName { get; }
		int CurrentProcessId { get; }

		void LaunchCommand(string command, string arguments = null);

		void CloseAllDuplicateProcessesExceptCurrent();
		void CloseCurrentProcess();
	}
}