namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System.Threading.Tasks;

	using Controls.Window.Interfaces;

	using Infrastructure.Environment.Interfaces;

	using Interfaces;

	using Processes.Interfaces;

	using Web.Updates.Interfaces;

	class DefaultArgumentProcessor : INoArgumentProcessor
	{
		readonly IProcessManager processManager;
		readonly IInstallWindow installWindow;
		readonly IEnvironmentInformation environmentInformation;
		readonly IMaintenanceWindow maintenanceWindow;
		readonly IUpdateService updateService;

		public bool Terminates => ShouldInstall;

		bool ShouldInstall => !IsCurrentlyRunningFromInstallationFolder && !environmentInformation.GetIsDebugging();
		bool IsCurrentlyRunningFromInstallationFolder => processManager.GetCurrentProcessDirectory() == InstallArgumentProcessor.TargetDirectory;

		public DefaultArgumentProcessor(
			IProcessManager processManager,
			IInstallWindow installWindow,
			IEnvironmentInformation environmentInformation,
			IMaintenanceWindow maintenanceWindow,
			IUpdateService updateService)
		{
			this.processManager = processManager;
			this.installWindow = installWindow;
			this.environmentInformation = environmentInformation;
			this.maintenanceWindow = maintenanceWindow;
			this.updateService = updateService;
		}

		public bool CanProcess()
		{
			return true;
		}

		public async Task ProcessAsync()
		{
			if (!IsCurrentlyRunningFromInstallationFolder)
				maintenanceWindow.Show("Searching for updates ...");

			if (await updateService.UpdateAsync())
				return;

			maintenanceWindow.Hide();

			if (ShouldInstall)
				installWindow.Show();
		}
	}
}
