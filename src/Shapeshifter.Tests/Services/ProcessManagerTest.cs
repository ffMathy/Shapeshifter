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

            using (var processManager = container.Resolve<IProcessManager>())
            {
                processManager.LaunchCommand("timeout", "/t -1 /nobreak");

                var runningProcessesBeforeDisposal = Process.GetProcessesByName("timeout");
                Assert.AreEqual(1, runningProcessesBeforeDisposal.Length);
            }

            var runningProcessesAfterDisposal = Process.GetProcessesByName("timeout");
            Assert.AreEqual(0, runningProcessesAfterDisposal.Length);
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