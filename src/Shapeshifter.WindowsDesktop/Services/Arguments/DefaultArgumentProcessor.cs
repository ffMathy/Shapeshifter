namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System.Threading.Tasks;

	using Controls.Window.Interfaces;

	using Information;

	using Infrastructure.Environment.Interfaces;

	using Interfaces;

	using Web.Updates.Interfaces;

	class DefaultArgumentProcessor : INoArgumentProcessor
	{
		readonly IInstallWindow installWindow;
		readonly IEnvironmentInformation environmentInformation;
		readonly IMaintenanceWindow maintenanceWindow;
		readonly IUpdateService updateService;

		public bool Terminates => ShouldInstall;

		bool ShouldInstall => 
			!IsCurrentlyRunningFromInstallationFolder && !environmentInformation.GetIsDebugging();

		static bool IsCurrentlyRunningFromInstallationFolder => 
			CurrentProcessInformation.GetCurrentProcessDirectory() == InstallationInformation.TargetDirectory;

		public DefaultArgumentProcessor(
			IInstallWindow installWindow,
			IEnvironmentInformation environmentInformation,
			IMaintenanceWindow maintenanceWindow,
			IUpdateService updateService)
		{
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
