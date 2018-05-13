namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System;
	using System.Diagnostics;
	using System.Threading.Tasks;

    using Interfaces;
	using Serilog;
	using Services.Arguments.Interfaces;
    using Services.Processes.Interfaces;

    class StartupPreparationOperation: IStartupPreparationOperation
    {
        readonly IProcessManager processManager;
        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;
		readonly ILogger logger;

		bool hasRunBefore;

        public bool ShouldTerminate
            => aggregateArgumentProcessor.ShouldTerminate;

        public string[] Arguments { get; set; }

        public StartupPreparationOperation(
            IProcessManager processManager,
            IAggregateArgumentProcessor aggregateArgumentProcessor,
			ILogger logger)
        {
            this.processManager = processManager;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
			this.logger = logger;
		}

        public async Task RunAsync()
        {
			try
			{
				logger.Verbose("Invoking the startup preparation operation.");

				if (hasRunBefore)
				{
					throw new InvalidOperationException(
						"Can't run the startup preparation twice.");
				}

				if (Arguments == null)
				{
					throw new ArgumentNullException(
						nameof(Arguments));
				}

				hasRunBefore = true;

				processManager.CloseAllDuplicateProcessesExceptCurrent();
				await Task.Delay(1000);

				await aggregateArgumentProcessor.ProcessArgumentsAsync(Arguments);

				logger.Verbose("Startup preparation operation completed.");
			}
			catch (Exception ex)
			{
				Program.OnGlobalErrorOccured(ex);
			}
		}
    }
}