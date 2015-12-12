namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    using System;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;

    [TestClass]
    public class ProcessManagerTest: TestBase
    {
        [TestMethod]
        public void CanLaunchProcessesAndKillThemAfterDisposing()
        {
            var container = CreateContainer();

            var initialRunningProcesses = Process.GetProcessesByName("timeout")
                                                 .Length;

            using (var processManager = container.Resolve<IProcessManager>())
            {
                processManager.LaunchCommand("timeout", "/t -1 /nobreak");

                var runningProcessesBeforeDisposal = Process.GetProcessesByName("timeout").Length;
                Assert.AreEqual(initialRunningProcesses + 1, runningProcessesBeforeDisposal);
            }

            var runningProcessesAfterDisposal = Process.GetProcessesByName("timeout").Length;
            Assert.AreEqual(initialRunningProcesses, runningProcessesAfterDisposal);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowsExceptionWhenLaunchingFileThatDoesNotExist()
        {
            var container = CreateContainer();

            var processManager = container.Resolve<IProcessManager>();
            processManager.LaunchFile("foobar.txt");
        }
    }
}