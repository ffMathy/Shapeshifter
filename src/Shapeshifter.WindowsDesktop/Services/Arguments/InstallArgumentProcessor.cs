namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography.X509Certificates;

	using Controls.Window.ViewModels.Interfaces;

	using Infrastructure.Environment.Interfaces;

	using Interfaces;

	using Keyboard.Interfaces;

	using Processes.Interfaces;

	using Properties;

	using Services.Interfaces;
	using Infrastructure.Dependencies;
	using Infrastructure.Threading.Interfaces;
	using Serilog;
	using System.Threading;

	class InstallArgumentProcessor : INoArgumentProcessor, IInstallArgumentProcessor
	{
		const string CertificateName = "Shapeshifter";

		readonly IProcessManager processManager;
		readonly ICertificateManager certificateManager;
		readonly IEnvironmentInformation environmentInformation;
		readonly ISettingsViewModel settingsViewModel;
		readonly IThreadDelay threadDelay;
		readonly ITrayIconManager trayIconManager;

		[Inject]
		public ILogger Logger { get; set; }

		public InstallArgumentProcessor(
			IProcessManager processManager,
			ICertificateManager certificateManager,
			IEnvironmentInformation environmentInformation,
			ISettingsViewModel settingsViewModel,
			IThreadDelay threadDelay,
			ITrayIconManager trayIconManager)
		{
			this.processManager = processManager;
			this.certificateManager = certificateManager;
			this.environmentInformation = environmentInformation;
			this.settingsViewModel = settingsViewModel;
			this.threadDelay = threadDelay;
			this.trayIconManager = trayIconManager;
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

		static string TargetExecutableFile
		{
			get
			{
				return Path.Combine(TargetDirectory, "Shapeshifter.exe");
			}
		}

		public bool CanProcess()
		{
			var isDebugging = environmentInformation.GetIsDebugging();
			if (isDebugging)
				return false;

			return !GetIsCurrentlyRunningFromInstallationFolder();
		}

		private bool GetIsCurrentlyRunningFromInstallationFolder()
		{
			return processManager.GetCurrentProcessDirectory() == TargetDirectory;
		}

		private static bool DoesTargetExecutableExist()
		{
			return File.Exists(TargetExecutableFile);
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

			trayIconManager.DisplayInformation("Shapeshifter installed", "Install location: " + TargetDirectory);
			LaunchInstalledExecutable(
				processManager.GetCurrentProcessFilePath());

			Logger.Information("Launched installed executable.");
		}

		void InstallToInstallDirectory()
		{
			WriteExecutable();
			WriteApplicationConfiguration();
			Logger.Information("Executable and manifest written to install directory.");
		}

		void WriteApplicationConfiguration()
		{
			File.WriteAllText(
				Resources.App,
				Path.Combine(
					TargetDirectory, 
					"Shapeshifter.config"));
		}

		void ConfigureDefaultSettings()
		{
			settingsViewModel.StartWithWindows = true;
		}

		X509Certificate2 InstallCertificateIfNotFound()
		{
			var existingCertificates = certificateManager.GetCertificatesByIssuerFromStore(
				$"CN={CertificateName}",
				StoreName.My,
				StoreLocation.LocalMachine);
			if (existingCertificates.Any())
			{
				try
				{
					return existingCertificates.Single();
				}
				finally
				{
					Logger.Information("Using existing code signing certificate.");
				}
			}

			try
			{
				return InstallCodeSigningCertificate();
			}
			finally
			{
				Logger.Information("Installed new code signing certificate.");
			}
		}

		X509Certificate2 InstallCodeSigningCertificate()
		{
			var certificate = certificateManager.GenerateSelfSignedCertificate(
				$"CN={CertificateName}");
			certificateManager.InstallCertificateToStore(
				certificate,
				StoreName.My,
				StoreLocation.LocalMachine);
			certificateManager.InstallCertificateToStore(
				certificate,
				StoreName.Root,
				StoreLocation.LocalMachine);

			return certificate;
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
