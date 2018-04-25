namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
	using System;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using Controls.Window.Factories.Interfaces;

	using Infrastructure.Events;

	using Interfaces;

	using Services.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Files;
	using Shapeshifter.WindowsDesktop.Services.Files.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;
	using Application = System.Windows.Application;

	public class TrayPreparationOperation : ITrayPreparationOperation
	{
		readonly ITrayIconManager trayIconManager;
		readonly ISettingsWindowFactory settingsWindowFactory;
		readonly IProcessManager processManager;
		readonly ISettingsManager settingsManager;

		public TrayPreparationOperation(
			ITrayIconManager trayIconManager,
			ISettingsWindowFactory settingsWindowFactory,
			IProcessManager processManager,
			ISettingsManager settingsManger)
		{
			this.trayIconManager = trayIconManager;
			this.settingsWindowFactory = settingsWindowFactory;
			this.processManager = processManager;
			this.settingsManager = settingsManger;
		}

		public async Task RunAsync()
		{
			var assemblyName = typeof(TrayPreparationOperation).Assembly.GetName();

			trayIconManager.IconClicked += TrayIconManager_IconClicked;
			trayIconManager.UpdateHoverText("Shapeshifter version " + assemblyName.Version);
			trayIconManager.UpdateMenuItems(
				"Settings",
				new[]
				{
					new MenuItem(
						"Exit",
						(sender, e) => Application.Current.Shutdown()),
					new MenuItem(
						"Show log file",
						(sender, e) => processManager.LaunchFile(
							FileManager.GetFullPathFromTemporaryPath(
								"Shapeshifter.log")))
				});

			trayIconManager.DisplayInformation("Shapeshifter is ready", "You can now use Shapeshifter for managing your clipboard.");

			settingsManager.SaveSetting("LastLoad", DateTime.UtcNow);
		}

		void TrayIconManager_IconClicked(object sender, TrayIconClickedEventArgument e)
		{
			var window = settingsWindowFactory.Create();
			window.Show();
		}
	}
}