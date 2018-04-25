namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;
	using Serilog;
	using Services.Web.Updates.Interfaces;

    public class PostPreparationOperation: IPostPreparationOperation
    {
        readonly IUpdateService updateService;
        readonly IEnvironmentInformation environmentInformation;
        readonly ITrayPreparationOperation trayPreparationOperation;
		readonly ILogger logger;

		public PostPreparationOperation(
            IUpdateService updateService,
            IEnvironmentInformation environmentInformation,
            ITrayPreparationOperation trayPreparationOperation,
			ILogger logger)
        {
            this.updateService = updateService;
            this.environmentInformation = environmentInformation;
            this.trayPreparationOperation = trayPreparationOperation;
			this.logger = logger;
		}

        public async Task RunAsync()
		{
			logger.Verbose("Invoking the post preparation operation.");

			await Task.WhenAll(
                trayPreparationOperation.RunAsync(),
                Update());
        }

        async Task Update()
        {
            if (!environmentInformation.GetIsDebugging() && environmentInformation.GetHasInternetAccess())
            {
                await updateService.UpdateAsync();
            }
        }
    }
}