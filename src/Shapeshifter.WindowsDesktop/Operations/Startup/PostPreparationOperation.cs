namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using Services.Updates.Interfaces;

    public class PostPreparationOperation: IPostPreparationOperation
    {
        readonly IUpdateService updateService;
        readonly IEnvironmentInformation environmentInformation;
        readonly ITrayPreparationOperation trayPreparationOperation;

        public PostPreparationOperation(
            IUpdateService updateService,
            IEnvironmentInformation environmentInformation,
            ITrayPreparationOperation trayPreparationOperation)
        {
            this.updateService = updateService;
            this.environmentInformation = environmentInformation;
            this.trayPreparationOperation = trayPreparationOperation;
        }

        public async Task RunAsync()
        {
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