namespace Shapeshifter.WindowsDesktop.Services
{
    using System.ComponentModel;
    using System.Diagnostics;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Processes.Interfaces;

    [TestClass]
    public class ProcessManagerTest: UnitTestFor<IProcessManager>
    {
        [Ignore]
        [TestMethod]
        [TestCategory("Integration")]
        public void CanLaunchProcessesAndKillThemAfterDisposing()
        {
            var initialRunningProcesses = GetTimeoutProcesses()
                .Length;
            using (SystemUnderTest)
            {
                SystemUnderTest.LaunchCommand("timeout", "/t -1 /nobreak");

                var runningProcessesBeforeDisposal = GetTimeoutProcesses()
                    .Length;
                Extensions.AssertWait(
                    () =>
                    Assert.AreEqual(initialRunningProcesses + 1, runningProcessesBeforeDisposal));
            }

            var runningProcessesAfterDisposal = GetTimeoutProcesses()
                .Length;
            Extensions.AssertWait(
                () =>
                Assert.AreEqual(initialRunningProcesses, runningProcessesAfterDisposal));
        }

        static Process[] GetTimeoutProcesses()
        {
            return Process.GetProcessesByName("timeout");
        }

        [TestMethod]
        [ExpectedException(typeof (Win32Exception))]
        public void ThrowsExceptionWhenLaunchingFileThatDoesNotExist()
        {
            SystemUnderTest.LaunchFile("foobar.txt");
        }
    }
}