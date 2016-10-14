namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;
    using Services.Processes.Interfaces;

    [TestClass]
    public class SettingsViewModelTest: UnitTestFor<ISettingsViewModel>
    {
        static string GetRunRegistryPath()
        {
            return @"Software\Microsoft\Windows\CurrentVersion\Run";
        }

        [TestMethod]
        public void StartWithWindowsIsTrueWhenRunKeyIsAdded()
        {
            Container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns("somePath");
            
            Assert.IsTrue(SystemUnderTest.StartWithWindows);
        }

        [TestMethod]
        public void StartWithWindowsIsFalseWhenRunKeyIsNotPresent()
        {
            Container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns((string)null);
            
            Assert.IsFalse(SystemUnderTest.StartWithWindows);
        }

        [TestMethod]
        public void ChangingStartWithWindowsToTrueWritesRunKey()
        {
            Container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns((string)null);

            Container
                .Resolve<IProcessManager>()
                .GetCurrentProcessFilePath()
                .Returns("executablePath");
            
            SystemUnderTest.StartWithWindows = true;

            Container
                .Resolve<IRegistryManager>()
                .Received()
                .AddValue(
                    GetRunRegistryPath(),
                    "Shapeshifter",
                    @"""executablePath""");
        }

        [TestMethod]
        public void ChangingStartWithWindowsToFalseRemovesRunKey()
        {
            Container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns("somePath");

            SystemUnderTest.StartWithWindows = false;

            Container
                .Resolve<IRegistryManager>()
                .Received()
                .RemoveValue(
                    GetRunRegistryPath(),
                    "Shapeshifter");
        }
    }
}