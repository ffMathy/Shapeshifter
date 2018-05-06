namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using Controls.Window.Interfaces;

	using Infrastructure.Environment.Interfaces;

	using Interfaces;

	using Processes.Interfaces;

	class DefaultArgumentProcessor: INoArgumentProcessor
	{
		readonly IProcessManager processManager;
		readonly IInstallWindow installWindow;
		readonly IEnvironmentInformation environmentInformation;

		public bool Terminates => CanProcess();

		public DefaultArgumentProcessor(
			IProcessManager processManager,
			IInstallWindow installWindow,
			IEnvironmentInformation environmentInformation)
		{
			this.processManager = processManager;
			this.installWindow = installWindow;
			this.environmentInformation = environmentInformation;
		}

		public bool CanProcess()
		{
			return !GetIsCurrentlyRunningFromInstallationFolder() && !environmentInformation.GetIsDebugging();
		}

		public void Process()
		{
			installWindow.Show();
		}

		bool GetIsCurrentlyRunningFromInstallationFolder()
		{
			return processManager.GetCurrentProcessDirectory() == InstallArgumentProcessor.TargetDirectory;
		}
	}
}
