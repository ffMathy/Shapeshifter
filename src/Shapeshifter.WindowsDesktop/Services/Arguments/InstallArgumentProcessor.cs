namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography.X509Certificates;

	using Controls.Window.ViewModels.Interfaces;

	using Infrastructure.Environment.Interfaces;
	using Infrastructure.Signing.Interfaces;

	using Interfaces;

	using Keyboard.Interfaces;

	using Processes.Interfaces;

	using Properties;

	using Services.Interfaces;
	using Infrastructure.Dependencies;
	using Infrastructure.Threading.Interfaces;
	using Serilog;

	class InstallArgumentProcessor : INoArgumentProcessor, IInstallArgumentProcessor
    {
        const string CertificateName = "Shapeshifter";

        readonly IProcessManager processManager;
        readonly ICertificateManager certificateManager;
        readonly ISignHelper signHelper;
        readonly IEnvironmentInformation environmentInformation;
        readonly ISettingsViewModel settingsViewModel;
        readonly IKeyboardDominanceWatcher keyboardDominanceWatcher;
        readonly IThreadDelay threadDelay;

        [Inject]
        public ILogger Logger { get; set; }

        public InstallArgumentProcessor(
            IProcessManager processManager,
            ICertificateManager certificateManager,
            ISignHelper signHelper,
            IEnvironmentInformation environmentInformation,
            ISettingsViewModel settingsViewModel,
            IKeyboardDominanceWatcher keyboardDominanceWatcher,
            IThreadDelay threadDelay)
        {
            this.processManager = processManager;
            this.certificateManager = certificateManager;
            this.signHelper = signHelper;
            this.environmentInformation = environmentInformation;
            this.settingsViewModel = settingsViewModel;
            this.keyboardDominanceWatcher = keyboardDominanceWatcher;
            this.threadDelay = threadDelay;
        }

        public bool Terminates
            => CanProcess();

        static string TargetDirectory
        {
            get
            {
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                return Path.Combine(programFilesPath, "Shapeshifter");
            }
        }

        public bool CanProcess()
        {
            return !environmentInformation.GetIsDebugging() &&
                   (processManager.GetCurrentProcessDirectory() != TargetDirectory);
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

            var currentExecutableFile = processManager.GetCurrentProcessFilePath();
            var targetExecutableFile = Path.Combine(TargetDirectory, "Shapeshifter.exe");

            InstallToInstallDirectory(targetExecutableFile);
            SignAssembly(targetExecutableFile);

            ConfigureDefaultSettings();

            Logger.Information("Default settings have been configured.");
            Logger.Information("Configuring keyboard dominance watcher injection mechanism.");

            keyboardDominanceWatcher.Install();

            Logger.Information("Injection mechanism installed and configured in the Global Assembly Cache.");

            LaunchInstalledExecutable(targetExecutableFile, currentExecutableFile);

            Logger.Information("Launched installed executable.");
        }

        void SignAssembly(string targetExecutableFile)
        {
            var selfSignedCertificate = InstallCertificateIfNotFound();
            signHelper.SignAssemblyWithCertificate(targetExecutableFile, selfSignedCertificate);

            threadDelay.Execute(1000);

            Logger.Information("Executable signed with newly created self-signing certificate.");
        }

        void InstallToInstallDirectory(string targetExecutableFile)
        {
            WriteManifest(targetExecutableFile);
            WriteExecutable(targetExecutableFile);

            threadDelay.Execute(1000);

            Logger.Information("Executable and manifest written to install directory.");
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

        void LaunchInstalledExecutable(string targetExecutableFile, string currentExecutableFile)
        {
            processManager.LaunchFileWithAdministrativeRights(targetExecutableFile, $"cleanup \"{currentExecutableFile}\"");
        }

        void WriteExecutable(string targetExecutableFile)
        {
            File.Copy(
                processManager.GetCurrentProcessFilePath(),
                targetExecutableFile,
                true);
        }

        static void WriteManifest(string targetExecutableFile)
        {
            var targetManifestFile = $"{targetExecutableFile}.manifest";
            File.WriteAllBytes(
                targetManifestFile,
                Resources.App);
        }

        void PrepareInstallDirectory()
        {
            Logger.Information("Target install directory is " + TargetDirectory + ".");

            if (Directory.Exists(TargetDirectory))
            {
                Directory.Delete(TargetDirectory, true);
            }

            Directory.CreateDirectory(TargetDirectory);

            Logger.Information("Install directory prepared.");
        }
    }
}