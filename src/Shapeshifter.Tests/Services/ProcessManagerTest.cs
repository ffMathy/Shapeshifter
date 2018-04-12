namespace Shapeshifter.WindowsDesktop.Services
{
    using System.ComponentModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Processes.Interfaces;

    [TestClass]
    public class ProcessManagerTest: UnitTestFor<IProcessManager>
    {
        [TestMethod]
        [ExpectedException(typeof (Win32Exception))]
        public void ThrowsExceptionWhenLaunchingFileThatDoesNotExist()
        {
            SystemUnderTest.LaunchFile("foobar.txt");
        }
    }
}