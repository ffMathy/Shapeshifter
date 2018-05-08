namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
	using System;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using Controls.Window.Factories.Interfaces;

	using Infrastructure.Events;

	using Interfaces;

	using Services.Interfaces;
	using Services.Files;
	using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Web.Updates.Interfaces;
	using Application = System.Windows.Application;

	public class TrayPreparationOperation : ITrayPreparationOperation
	{
		readonly ITrayIconManager trayIconManager;
		readonly ISettingsWindowFactory settingsWindowFactory;
		readonly IProcessManager processManager;
		readonly ISettingsManager settingsManager;
		readonly IUpdateService updateService;

		public TrayPreparationOperation(
			ITrayIconManager trayIconManager,
			ISettingsWindowFactory settingsWindowFactory,
			IProcessManager processManager,
			ISettingsManager settingsManger,
			IUpdateService updateService)
		{
			this.trayIconManager = trayIconManager;
			this.settingsWindowFactory = settingsWindowFactory;
			this.processManager = processManager;
			this.settingsManager = settingsManger;
			this.updateService = updateService;
		}

		public async Task RunAsync()
		{
			var assemblyName = typeof(TrayPreparationOperation).Assembly.GetName();

			trayIconManager.IconDoubleClicked += TrayIconManager_IconClicked;
			trayIconManager.UpdateHoverText("Shapeshifter version " + assemblyName.Version);
			trayIconManager.UpdateMenuItems(
				"Settings",
				new[]
				{
					new MenuItem(
						"Show log files",
						(sender, e) => processManager.LaunchFile("explorer", "\"" + FileManager.GetFullPathFromTemporaryPath() + "\"")),
					new MenuItem(
						"Search for updates",
						(sender, e) => {
							trayIconManager.DisplayInformation("Looking for updates", "You'll be notified if any are found.");
							updateService.UpdateAsync();
						}),
					new MenuItem(
						"Exit",
						(sender, e) => Application.Current.Shutdown())
				});
			
			settingsManager.SaveSetting("LastLoad", DateTime.UtcNow);
		}

		void TrayIconManager_IconClicked(object sender, TrayIconDoubleClickedEventArgument e)
		{
			var window = settingsWindowFactory.Create();
			window.Show();
		}
	}
}