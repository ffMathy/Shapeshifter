namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;

    using Interfaces;
    using Services.Interfaces;

    class InstallArgumentProcessor : INoArgumentProcessor
    {
        readonly IProcessManager processManager;

        public InstallArgumentProcessor(
            IProcessManager processManager)
        {
            this.processManager = processManager;
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
            return Environment.CurrentDirectory == TargetDirectory;
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
            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
            }

            var targetExecutableFile = Path.Combine(TargetDirectory, "Shapeshifter.exe");
            var currentExecutableFile = processManager.GetCurrentProcessPath();

            File.Copy(processManager.GetCurrentProcessPath(), targetExecutableFile, true);
            processManager.LaunchFileWithAdministrativeRights(targetExecutableFile, $"cleanup \"{currentExecutableFile}\"");
        }
    }
}