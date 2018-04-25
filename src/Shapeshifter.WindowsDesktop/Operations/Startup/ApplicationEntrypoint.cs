namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;
	using Serilog;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;

	public class ApplicationEntrypoint: ISingleInstance
    {
        readonly IStartupPreparationOperation startupPreparationOperation;
        readonly IPostPreparationOperation postPreparationOperation;
        readonly IMainWindowPreparationOperation mainWindowPreparationOperation;
		private readonly IProcessManager processManager;
		readonly ILogger logger;

		public ApplicationEntrypoint(
            IStartupPreparationOperation startupPreparationOperation,
            IPostPreparationOperation postPreparationOperation,
            IMainWindowPreparationOperation mainWindowPreparationOperation,
			IProcessManager processManager,
			ILogger logger)
        {
            this.startupPreparationOperation = startupPreparationOperation;
            this.postPreparationOperation = postPreparationOperation;
            this.mainWindowPreparationOperation = mainWindowPreparationOperation;
			this.processManager = processManager;
			this.logger = logger;
		}

        public async Task Start(
            params string[] arguments)
        {
            await Prepare(arguments);
            if (startupPreparationOperation.ShouldTerminate)
            {
				logger.Verbose("The startup preparation operation signalled a termination request. Will quit process.");
				processManager.CloseCurrentProcess();
				return;
            }

            await postPreparationOperation.RunAsync();
            await mainWindowPreparationOperation.RunAsync();
        }

        async Task Prepare(string[] arguments)
        {
            startupPreparationOperation.Arguments = arguments;
            await startupPreparationOperation.RunAsync();
        }
    }
}