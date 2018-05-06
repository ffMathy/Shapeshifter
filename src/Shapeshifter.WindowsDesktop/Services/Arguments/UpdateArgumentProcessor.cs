namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System.Linq;
	using System.Threading.Tasks;
	using Interfaces;
	using Serilog;

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
			var targetDirectory = fileManager.PrepareIsolatedFolder();
			await fileManager.DeleteDirectoryIfExistsAsync(targetDirectory);

			processManager.LaunchFile(processManager.GetCurrentProcessFilePath());
		}
    }
}