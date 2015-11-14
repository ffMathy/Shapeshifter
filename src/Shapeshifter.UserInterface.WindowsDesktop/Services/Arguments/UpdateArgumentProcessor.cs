#region

using System;
using System.IO;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsProcess = System.Diagnostics.Process;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments
{
    internal class UpdateArgumentProcessor : IArgumentProcessor
    {
        private readonly IProcessManager processManager;

        public UpdateArgumentProcessor(
            IProcessManager processManager)
        {
            this.processManager = processManager;
        }

        public bool Terminates
            => true;

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("update");
        }

        public void Process(string[] arguments)
        {
            var updateIndex = Array.IndexOf(arguments, "update");
            var targetDirectory = arguments[updateIndex + 1];

            var currentDirectory = Environment.CurrentDirectory;
            InstallNewVersion(targetDirectory, currentDirectory);
        }

        private void InstallNewVersion(string targetDirectory, string currentDirectory)
        {
            foreach (var currentFile in Directory.GetFiles(currentDirectory))
            {
                HandleNewFile(targetDirectory, currentFile);
            }

            LaunchNewExecutable(targetDirectory);
        }

        private void LaunchNewExecutable(string targetDirectory)
        {
            using (var currentProcess = WindowsProcess.GetCurrentProcess())
            {
                var executableFile = Path.Combine(targetDirectory, $"{currentProcess.ProcessName}.exe");
                processManager.LaunchFile(executableFile, $"cleanup {Environment.CurrentDirectory}");
            }
        }

        private static void HandleNewFile(string targetDirectory, string currentFile)
        {
            var currentFileName = Path.GetFileName(currentFile);

            var targetFile = Path.Combine(targetDirectory, currentFileName);
            DeleteFileIfExists(targetFile);

            File.Copy(currentFile, targetFile);
        }

        private static void DeleteFileIfExists(string targetFile)
        {
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
        }
    }
}