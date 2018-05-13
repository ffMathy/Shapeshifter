namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
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
			
			var isOriginFromInstallPath = origin == null || origin.StartsWith(InstallArgumentProcessor.TargetDirectory);
			if (!isOriginFromInstallPath)
			{
				await fileManager.DeleteFileIfExistsAsync(origin);
				await fileManager.CopyFileAsync(processManager.GetCurrentProcessFilePath(), origin);

				processManager.LaunchFileWithAdministrativeRights(origin);
			}
			else
			{
				processManager.LaunchFileWithAdministrativeRights(processManager.GetCurrentProcessFilePath(), "install");
			}
		}
	}
}