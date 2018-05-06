namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

	using Controls.Window.Interfaces;
	using Controls.Window.ViewModels.Interfaces;

	using Interfaces;

	using Processes.Interfaces;

	using Properties;

	using Serilog;

	class InstallArgumentProcessor : IInstallArgumentProcessor
	{
		readonly IProcessManager processManager;
		readonly ISettingsViewModel settingsViewModel;
		readonly IMaintenanceWindow maintenanceWindow;
		readonly ILogger logger;

		public InstallArgumentProcessor(
			IProcessManager processManager,
			ISettingsViewModel settingsViewModel,
			IMaintenanceWindow maintenanceWindow,
			ILogger logger)
		{
			this.processManager = processManager;
			this.settingsViewModel = settingsViewModel;
			this.maintenanceWindow = maintenanceWindow;
			this.logger = logger;
		}

		public bool Terminates => true;

		internal static string TargetDirectory
		{
			get
			{
				var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return Path.Combine(programFilesPath, "Shapeshifter");
			}
		}

		static string TargetExecutableFile => Path.Combine(TargetDirectory, "Shapeshifter.exe");

		public bool CanProcess(string[] arguments) => arguments.Contains("install");

		public async Task ProcessAsync(string[] arguments)
		{
			logger.Information("Running installation procedure.");
			Install();
		}

		void Install()
		{
			maintenanceWindow.Show("Installing Shapeshifter ...");

			PrepareInstallDirectory();
			InstallToInstallDirectory();
			
			ConfigureDefaultSettings();

			logger.Information("Default settings have been configured.");

			LaunchInstalledExecutable(
				processManager.GetCurrentProcessFilePath());

			logger.Information("Launched installed executable.");
		}

		void InstallToInstallDirectory()
		{
			WriteExecutable();
			WriteApplicationConfiguration();
			WriteApplicationManifest();

			logger.Information("Executable, configuration and manifest written to install directory.");
		}

		static void WriteApplicationManifest()
		{
			File.WriteAllBytes(
				Path.Combine(
					TargetDirectory,
					"Shapeshifter.manifest"),
				Resources.AppManifest);
		}

		static void WriteApplicationConfiguration()
		{
			File.WriteAllText(
				Path.Combine(
					TargetDirectory,
					"Shapeshifter.config"),
				Resources.AppConfiguration);
		}

		void ConfigureDefaultSettings()
		{
			settingsViewModel.StartWithWindows = true;
		}

		void LaunchInstalledExecutable(string currentExecutableFile)
		{
			processManager.LaunchFileWithAdministrativeRights(TargetExecutableFile, $"postinstall \"{currentExecutableFile}\"");
		}

		void WriteExecutable()
		{
			File.Copy(
				processManager.GetCurrentProcessFilePath(),
				TargetExecutableFile,
				true);
		}

		void PrepareInstallDirectory()
		{
			logger.Information("Target install directory is " + TargetDirectory + ".");

			if (Directory.Exists(TargetDirectory))
				Directory.Delete(TargetDirectory, true);

			Directory.CreateDirectory(TargetDirectory);

			logger.Information("Install directory prepared.");
		}
	}
}
