namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System;
	using System.Diagnostics;

	using WindowsDesktop.Services.Processes.Interfaces;

    using Controls.Designer.Services;

    class DesignerProcessManager
        : IProcessManager,
          IDesignerService
    {
		public string CurrentProcessName => throw new NotImplementedException();

		public int CurrentProcessId => throw new NotImplementedException();

		public Process LaunchCommand(string command, string arguments = null)
		{
			throw new NotImplementedException();
		}

		public void CloseAllDuplicateProcessesExceptCurrent() { }

		public Process LaunchFile(string fileName, string arguments = null)
		{
			throw new NotImplementedException();
		}

		public Process LaunchFileWithAdministrativeRights(string fileName, string arguments = null)
		{
			throw new NotImplementedException();
		}

		public bool IsCurrentProcessElevated()
        {
            return false;
        }

        public void Dispose() { }

        public string GetCurrentProcessDirectory()
        {
            return null;
        }

		public string GetCurrentProcessFilePath()
		{
			throw new NotImplementedException();
		}

		public void CloseCurrentProcess()
		{
			throw new NotImplementedException();
		}
	}
}