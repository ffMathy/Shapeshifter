namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;
	using System.Linq;
	using Keyboard.Interfaces;
	using System.Threading.Tasks;
	using Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;
	using Shapeshifter.WindowsDesktop.Services.Files.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;

	class PostInstallArgumentProcessor : ISingleArgumentProcessor
	{
		readonly ILogger logger;
		readonly IFileManager fileManager;
		readonly IKeyboardDominanceWatcher keyboardDominanceWatcher;
		readonly IProcessManager processManager;

		public bool Terminates => true;

		public PostInstallArgumentProcessor(
			ILogger logger,
			IFileManager fileManager,
			IKeyboardDominanceWatcher keyboardDominanceWatcher,
			IProcessManager processManager)
		{
			this.logger = logger;
			this.fileManager = fileManager;
			this.keyboardDominanceWatcher = keyboardDominanceWatcher;
			this.processManager = processManager;
		}

		public bool CanProcess(string[] arguments)
		{
			return arguments.Contains("postinstall");
		}

		public async Task ProcessAsync(string[] arguments)
		{
			var updateIndex = Array.IndexOf(arguments, "postinstall");
			var targetDirectory = arguments[updateIndex + 1];

			logger.Information("Configuring keyboard dominance watcher injection mechanism.");
			keyboardDominanceWatcher.Install();

			logger.Information("Attempting to delete file {file}.", targetDirectory);
			await fileManager.DeleteFileIfExistsAsync(targetDirectory);
			logger.Verbose("Old file has been cleaned up.");

			processManager.LaunchFileWithAdministrativeRights(processManager.GetCurrentProcessFilePath());
		}
	}
}
