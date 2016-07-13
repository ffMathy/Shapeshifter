namespace Shapeshifter.WindowsDesktop.Services.Processes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Principal;

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

        public string GetCurrentProcessName()
        {
            using (var currentProcess = Process.GetCurrentProcess())
                return
                    $"{currentProcess.ProcessName}";
        }

        public string GetCurrentProcessPath()
        {
            return Path.Combine(
                Environment.CurrentDirectory,
                $"{GetCurrentProcessName()}.exe");
        }

        public void LaunchCommand(string command, string arguments = null)
        {
            SpawnProcess(command, Environment.CurrentDirectory);
        }

        public void CloseAllDuplicateProcessesExceptCurrent()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        static void CloseProcessesExceptProcessWithId(
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

        static void CloseProcess(Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }

                if (CloseMainWindow(process))
                {
                    return;
                }

                process.Kill();
            }
            finally
            {
                process.Dispose();
            }
        }

        static bool CloseMainWindow(Process process)
        {
            process.CloseMainWindow();
            if (process.WaitForExit(3000))
            {
                return true;
            }
            return false;
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

        void SpawnProcess(string uri, string workingDirectory, string verb = null)
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = uri,
                    WorkingDirectory = workingDirectory,
                    Verb = verb
                });
            processes.Add(process);
        }
    }
}