namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading.Tasks;

	using Information;

	using Interfaces;

	using Shapeshifter.WindowsDesktop.Services.Files.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;

	class UpdateArgumentProcessor : ISingleArgumentProcessor
	{
		readonly IFileManager fileManager;
		readonly IProcessManager processManager;

		public UpdateArgumentProcessor(
			IFileManager fileManager,
			IProcessManager processManager)
		{
			this.fileManager = fileManager;
			this.processManager = processManager;
		}

		public bool Terminates => true;

		public bool CanProcess(string[] arguments)
		{
			return arguments.Contains("update");
		}

		public async Task ProcessAsync(string[] arguments)
		{
			string origin = null;

			var originIndex = Array.IndexOf(arguments, "update") + 1;
			if (originIndex != arguments.Length)
			{
				origin = arguments[originIndex];
			}

			var isOriginFromInstallPath = origin == null || origin.StartsWith(InstallationInformation.TargetDirectory);
			if (!isOriginFromInstallPath)
			{
				await fileManager.DeleteFileIfExistsAsync(origin);
				await fileManager.CopyFileAsync(CurrentProcessInformation.GetCurrentProcessFilePath(), origin);

				try
				{
					processManager.LaunchFileWithAdministrativeRights(origin);
				}
				catch (Win32Exception)
				{
					//the user cancelled the update operation.
				}
			}
			else
			{
				try
				{
					processManager.LaunchFileWithAdministrativeRights(CurrentProcessInformation.GetCurrentProcessFilePath(), "install");
				}
				catch (Win32Exception)
				{
					//the user cancelled the update operation.
				}
			}
		}
	}
}