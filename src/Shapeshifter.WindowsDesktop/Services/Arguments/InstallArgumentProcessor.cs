namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    using Interfaces;

    using Native.Helpers.Interfaces;

    using Properties;

    using Services.Interfaces;

    class InstallArgumentProcessor : INoArgumentProcessor
    {
        readonly IProcessManager processManager;
        readonly ICertificateManager certificateManager;
        readonly ISignHelper signHelper;

        public InstallArgumentProcessor(
            IProcessManager processManager,
            ICertificateManager certificateManager,
            ISignHelper signHelper)
        {
            this.processManager = processManager;
            this.certificateManager = certificateManager;
            this.signHelper = signHelper;
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
            return Environment.CurrentDirectory != TargetDirectory;
        }

        public void Process()
        {
            if (!processManager.IsCurrentProcessElevated())
            {
                processManager.LaunchFileWithAdministrativeRights(
                    processManager.GetCurrentProcessPath());
            }
            else
            {
                Install();
            }
        }

        void Install()
        {
            PrepareInstallDirectory();

            var currentExecutableFile = processManager.GetCurrentProcessPath();
            var targetExecutableFile = Path.Combine(TargetDirectory, "Shapeshifter.exe");

            WriteManifest(targetExecutableFile);
            WriteExecutable(targetExecutableFile);

            var rootCertificate = InstallRootCertificateIfNotFound();
            signHelper.SignAssemblyWithCertificate(targetExecutableFile, rootCertificate);

            LaunchInstalledExecutable(targetExecutableFile, currentExecutableFile);
        }

        X509Certificate2 InstallRootCertificateIfNotFound()
        {
            const string name = "CN=Shapeshifter";
            var existingCertificates = certificateManager.GetCertificatesByIssuerFromStore(
                name,
                StoreName.Root,
                StoreLocation.LocalMachine);
            if (!existingCertificates.Any())
            {
                return InstallRootCertificate(name);
            }
            else
            {
                return existingCertificates.First();
            }
        }

        X509Certificate2 InstallRootCertificate(string name)
        {
            var privateKey = certificateManager.GenerateCACertificate(name);
            var certificate = certificateManager.GenerateSelfSignedCertificate(name, name, privateKey);
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