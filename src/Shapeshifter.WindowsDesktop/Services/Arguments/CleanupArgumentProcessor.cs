namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;
    using System.Linq;

    using Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;

	class CleanupArgumentProcessor: ISingleArgumentProcessor
    {
		[Inject]
		public ILogger Logger { get; set; }

		public bool Terminates => false;

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("cleanup");
        }

        public void Process(string[] arguments)
        {
            var updateIndex = Array.IndexOf(arguments, "cleanup");
            var targetDirectory = arguments[updateIndex + 1];

			Logger.Information("Attempting to delete file {file}.", targetDirectory);
            File.Delete(targetDirectory);
			Logger.Verbose("Old file has been cleaned up.");
		}
    }
}