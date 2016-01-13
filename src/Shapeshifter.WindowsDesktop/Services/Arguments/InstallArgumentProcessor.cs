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
        const string CertificateAuthorityName = "Shapeshifter CA";
        const string CertificateName = "Shapeshifter";

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

            var selfSignedCertificate = InstallCertificatesIfNotFound();
            signHelper.SignAssemblyWithCertificate(targetExecutableFile, selfSignedCertificate);

            LaunchInstalledExecutable(targetExecutableFile, currentExecutableFile);
        }

        X509Certificate2 InstallCertificatesIfNotFound()
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
            var certificateAuthorityCertificate = certificateManager.GenerateCertificateAuthorityCertificate($"CN={CertificateAuthorityName}");
            var certificate = certificateManager.GenerateSelfSignedCertificate(
                $"CN={CertificateName}", $"CN={CertificateAuthorityName}", certificateAuthorityCertificate);
            certificateManager.InstallCertificateToStore(
                certificate,
                StoreName.My,
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