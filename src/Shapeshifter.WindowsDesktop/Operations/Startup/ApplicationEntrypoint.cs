namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;
	using Serilog;

	using Services.Interfaces;

	public class ApplicationEntrypoint: ISingleInstance
    {
        readonly IStartupPreparationOperation startupPreparationOperation;
        readonly ITrayPreparationOperation trayPreparationOperation;
        readonly IMainWindowPreparationOperation mainWindowPreparationOperation;
		readonly ILogger logger;

		public ApplicationEntrypoint(
            IStartupPreparationOperation startupPreparationOperation,
            ITrayPreparationOperation trayPreparationOperation,
            IMainWindowPreparationOperation mainWindowPreparationOperation,
			ILogger logger)
        {
            this.startupPreparationOperation = startupPreparationOperation;
            this.trayPreparationOperation = trayPreparationOperation;
            this.mainWindowPreparationOperation = mainWindowPreparationOperation;
			this.logger = logger;

			logger.Verbose("Application entry point initialized.");
		}

        public async Task Start(
            params string[] arguments)
        {
            await Prepare(arguments);
            if (startupPreparationOperation.ShouldTerminate)
            {
				logger.Verbose("The startup preparation operation signalled a termination request.");
				return;
            }

            await mainWindowPreparationOperation.RunAsync();
			await trayPreparationOperation.RunAsync();
        }

        async Task Prepare(string[] arguments)
        {
            startupPreparationOperation.Arguments = arguments;
            await startupPreparationOperation.RunAsync();
        }
    }
}