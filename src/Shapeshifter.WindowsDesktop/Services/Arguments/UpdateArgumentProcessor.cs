namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;
    using System.Linq;
	using System.Threading.Tasks;
	using Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;
	using Shapeshifter.WindowsDesktop.Services.Files.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;

	class UpdateArgumentProcessor : ISingleArgumentProcessor
    {
		readonly ILogger logger;
		readonly IFileManager fileManager;
		readonly IProcessManager processManager;

		public UpdateArgumentProcessor(
			ILogger logger,
			IFileManager fileManager,
			IProcessManager processManager)
		{
			this.logger = logger;
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
			var targetDirectory = fileManager.PrepareIsolatedFolder();
			await fileManager.DeleteDirectoryIfExistsAsync(targetDirectory);

			processManager.LaunchFile(processManager.GetCurrentProcessFilePath());
		}
    }
}