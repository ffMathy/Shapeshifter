namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;

	using Controls.Window.ViewModels.Interfaces;

	using Infrastructure.Environment.Interfaces;

	using Interfaces;

	using Processes.Interfaces;

	using Properties;

	using Infrastructure.Dependencies;

	using Serilog;

	using System.Windows;

	class InstallArgumentProcessor : INoArgumentProcessor, IInstallArgumentProcessor
	{
		readonly IProcessManager processManager;
		readonly IEnvironmentInformation environmentInformation;
		readonly ISettingsViewModel settingsViewModel;

		[Inject]
		public ILogger Logger { get; set; }

		public InstallArgumentProcessor(
			IProcessManager processManager,
			IEnvironmentInformation environmentInformation,
			ISettingsViewModel settingsViewModel)
		{
			this.processManager = processManager;
			this.environmentInformation = environmentInformation;
			this.settingsViewModel = settingsViewModel;
		}

		public bool Terminates => CanProcess() && !GetIsCurrentlyRunningFromInstallationFolder();

		static string TargetDirectory
		{
			get
			{
				var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return Path.Combine(programFilesPath, "Shapeshifter");
			}
		}

		static string TargetExecutableFile => Path.Combine(TargetDirectory, "Shapeshifter.exe");

		public bool CanProcess()
		{
			var isDebugging = environmentInformation.GetIsDebugging();
			if (isDebugging)
				return false;

			return !GetIsCurrentlyRunningFromInstallationFolder();
		}

		bool GetIsCurrentlyRunningFromInstallationFolder()
		{
			return processManager.GetCurrentProcessDirectory() == TargetDirectory;
		}

		public void Process()
		{
			if (!processManager.IsCurrentProcessElevated())
			{
				Logger.Information("Current process is not elevated which is needed for installation. Starting as elevated process.");
				processManager.LaunchFileWithAdministrativeRights(
					processManager.GetCurrentProcessFilePath());
			}
			else
			{
				Logger.Information("Current process is elevated.");
				Logger.Information("Running installation procedure.");
				Install();
			}
		}

		void Install()
		{
			PrepareInstallDirectory();
			InstallToInstallDirectory();
			
			ConfigureDefaultSettings();

			Logger.Information("Default settings have been configured.");

			MessageBox.Show(
				"Install location: " + TargetDirectory, 
				"Shapeshifter has been installed", 
				MessageBoxButton.OK, 
				MessageBoxImage.Information);

			LaunchInstalledExecutable(
				processManager.GetCurrentProcessFilePath());

			Logger.Information("Launched installed executable.");
		}

		void InstallToInstallDirectory()
		{
			WriteExecutable();
			WriteApplicationConfiguration();
			WriteApplicationManifest();

			Logger.Information("Executable, configuration and manifest written to install directory.");
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
			Logger.Information("Target install directory is " + TargetDirectory + ".");

			if (Directory.Exists(TargetDirectory))
				Directory.Delete(TargetDirectory, true);

			Directory.CreateDirectory(TargetDirectory);

			Logger.Information("Install directory prepared.");
		}
	}
}
