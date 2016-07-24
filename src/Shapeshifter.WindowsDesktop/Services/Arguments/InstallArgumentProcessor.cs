namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    using Controls.Window.ViewModels.Interfaces;

    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Signing.Interfaces;

    using Interfaces;

    using Keyboard.Interfaces;

    using Processes.Interfaces;

    using Properties;

    using Services.Interfaces;

    class InstallArgumentProcessor: INoArgumentProcessor
    {
        const string CertificateName = "Shapeshifter";

        readonly IProcessManager processManager;
        readonly ICertificateManager certificateManager;
        readonly ISignHelper signHelper;
        readonly IEnvironmentInformation environmentInformation;
        readonly ISettingsViewModel settingsViewModel;
        readonly IKeyboardDominanceWatcher keyboardDominanceWatcher;
        readonly ILogger logger;

        public InstallArgumentProcessor(
            IProcessManager processManager,
            ICertificateManager certificateManager,
            ISignHelper signHelper,
            IEnvironmentInformation environmentInformation,
            ISettingsViewModel settingsViewModel,
            IKeyboardDominanceWatcher keyboardDominanceWatcher,
            ILogger logger)
        {
            this.processManager = processManager;
            this.certificateManager = certificateManager;
            this.signHelper = signHelper;
            this.environmentInformation = environmentInformation;
            this.settingsViewModel = settingsViewModel;
            this.keyboardDominanceWatcher = keyboardDominanceWatcher;
            this.logger = logger;
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
                   (Environment.CurrentDirectory != TargetDirectory);
        }

        public void Process()
        {
            if (!processManager.IsCurrentProcessElevated())
            {
                logger.Information("Current process is not elevated which is needed for installation. Starting as elevated process.");
                processManager.LaunchFileWithAdministrativeRights(
                    processManager.GetCurrentProcessPath());
            }
            else
            {
                logger.Information("Current process is elevated.");
                logger.Information("Running installation procedure.");
                Install();
            }
        }

        void Install()
        {
            PrepareInstallDirectory();

            logger.Information("Install directory prepared.");

            var currentExecutableFile = processManager.GetCurrentProcessPath();
            var targetExecutableFile = Path.Combine(TargetDirectory, "Shapeshifter.exe");

            WriteManifest(targetExecutableFile);
            WriteExecutable(targetExecutableFile);

            logger.Information("Executable and manifest written to install directory.");

            var selfSignedCertificate = InstallCertificateIfNotFound();
            signHelper.SignAssemblyWithCertificate(targetExecutableFile, selfSignedCertificate);

            logger.Information("Executable signed with newly created self-signing certificate.");

            ConfigureDefaultSettings();

            logger.Information("Default settings have been configured.");

            keyboardDominanceWatcher.Install();

            logger.Information("Injection mechanism installed and configured in the Global Assembly Cache.");

            LaunchInstalledExecutable(targetExecutableFile, currentExecutableFile);

            logger.Information("Launched installed executable.");
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
            if (!existingCertificates.Any())
            {
                return InstallCodeSigningCertificate();
            }

            return existingCertificates.Single();
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
                processManager.GetCurrentProcessPath(),
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

        static void PrepareInstallDirectory()
        {
            if (Directory.Exists(TargetDirectory))
            {
                Directory.Delete(TargetDirectory, true);
            }

            Directory.CreateDirectory(TargetDirectory);
        }
    }
}