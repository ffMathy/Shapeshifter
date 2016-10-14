using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;
using System;
using System.IO;

namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    [TestClass]
    public class InstallArgumentProcessorTest: UnitTestFor<IInstallArgumentProcessor>
    {
        [TestMethod]
        public void CanProcessWhenIsNotDebuggingAndNotDeployed()
        {
            var fakeEnvironmentInformation = Get<IEnvironmentInformation>();
            fakeEnvironmentInformation.GetIsDebugging().Returns(false);

            var fakeProcessManager = Get<IProcessManager>();
            fakeProcessManager
                .GetCurrentProcessDirectory()
                .Returns(@"C:\foo");

            Assert.IsTrue(SystemUnderTest.CanProcess());
        }

        [TestMethod]
        public void CanNotProcessWhenIsDebuggingAndNotDeployed()
        {
            var fakeEnvironmentInformation = Get<IEnvironmentInformation>();
            fakeEnvironmentInformation.GetIsDebugging().Returns(true);

            var fakeProcessManager = Get<IProcessManager>();
            fakeProcessManager
                .GetCurrentProcessDirectory()
                .Returns(@"C:\foo");

            Assert.IsFalse(SystemUnderTest.CanProcess());
        }

        [TestMethod]
        public void CanNotProcessWhenIsNotDebuggingAndDeployed()
        {
            var fakeEnvironmentInformation = Get<IEnvironmentInformation>();
            fakeEnvironmentInformation.GetIsDebugging().Returns(false);

            var fakeProcessManager = Get<IProcessManager>();
            fakeProcessManager
                .GetCurrentProcessDirectory()
                .Returns(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Shapeshifter"));

            Assert.IsFalse(SystemUnderTest.CanProcess());
        }
    }
}
