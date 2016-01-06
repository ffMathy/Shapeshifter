namespace Shapeshifter.WindowsDesktop.Shared.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using Interfaces;

    class ProcessManager
        : IProcessManager
    {
        readonly ICollection<Process> processes;

        public ProcessManager()
        {
            processes = new HashSet<Process>();
        }

        public void Dispose()
        {
            foreach (var process in processes)
            {
                CloseProcess(process);
            }
        }

        public void LaunchCommand(string command, string arguments = null)
        {
            SpawnProcess(command, Environment.CurrentDirectory);
        }

        public void CloseAllProcessesExceptCurrent()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        static void CloseProcessesExceptProcessWithId(
            int processId,
            params Process[] processes)
        {
            foreach (var process in processes)
            {
                if (process.Id == processId)
                {
                    continue;
                }

                CloseProcess(process);
            }
        }

        static void CloseProcess(Process process)
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
            }
            process.Dispose();
        }

        public void LaunchFile(string fileName, string arguments = null)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The given file doesn't exist.", nameof(fileName));
            }

            var workingDirectory = Path.GetDirectoryName(fileName);
            SpawnProcess(fileName, workingDirectory);
        }

        void SpawnProcess(string fileName, string workingDirectory)
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory
                });
            processes.Add(process);
        }
    }
}