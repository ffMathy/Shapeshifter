namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Principal;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    class ProcessManager
        : IProcessManager
    {
        readonly IThreadDelay delay;

        readonly ICollection<Process> processes;

        public ProcessManager(IThreadDelay delay)
        {
            this.delay = delay;

            processes = new HashSet<Process>();
        }

        public void Dispose()
        {
            foreach (var process in processes)
            {
                CloseProcess(process);
            }
        }

        public string GetCurrentProcessPath()
        {
            throw new NotImplementedException();
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

        void CloseProcessesExceptProcessWithId(
            int processId,
            params Process[] targetProcesses)
        {
            foreach (var process in targetProcesses)
            {
                if (process.Id == processId)
                {
                    continue;
                }

                CloseProcess(process);
            }
        }

        void CloseProcess(Process process)
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
                delay.Execute(3000);
            }
            process.Dispose();
        }

        public void LaunchFile(string fileName, string arguments = null)
        {
            var workingDirectory = Path.GetDirectoryName(fileName);
            SpawnProcess(fileName, workingDirectory);
        }

        public void LaunchFileWithAdministrativeRights(string fileName, string arguments = null)
        {
            var workingDirectory = Path.GetDirectoryName(fileName);
            SpawnProcess(fileName, workingDirectory, "runas");
        }

        public bool IsCurrentProcessElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        void SpawnProcess(string fileName, string workingDirectory, string verb = null)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The given file doesn't exist.", nameof(fileName));
            }

            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory,
                    Verb = verb
                });
            processes.Add(process);
        }
    }
}