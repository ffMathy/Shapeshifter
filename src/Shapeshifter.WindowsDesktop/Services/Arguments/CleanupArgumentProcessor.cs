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

	class CleanupArgumentProcessor: ISingleArgumentProcessor
    {
		readonly ILogger logger;
		readonly IFileManager fileManager;

		public bool Terminates => false;

		public CleanupArgumentProcessor(
			ILogger logger,
			IFileManager fileManager)
		{
			this.logger = logger;
			this.fileManager = fileManager;
		}

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("cleanup");
        }

        public async Task ProcessAsync(string[] arguments)
        {
            var updateIndex = Array.IndexOf(arguments, "cleanup");
            var targetDirectory = arguments[updateIndex + 1];

			logger.Information("Attempting to delete file {file}.", targetDirectory);
			await fileManager.DeleteFileIfExistsAsync(targetDirectory);
			logger.Verbose("Old file has been cleaned up.");
		}
    }
}