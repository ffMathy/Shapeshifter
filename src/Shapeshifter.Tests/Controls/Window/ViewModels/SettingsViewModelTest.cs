namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;

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
            container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns("somePath");
            
            Assert.IsTrue(systemUnderTest.StartWithWindows);
        }

        [TestMethod]
        public void StartWithWindowsIsFalseWhenRunKeyIsNotPresent()
        {
            container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns((string)null);
            
            Assert.IsFalse(systemUnderTest.StartWithWindows);
        }

        [TestMethod]
        public void ChangingStartWithWindowsToTrueWritesRunKey()
        {
            container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns((string)null);

            container
                .Resolve<IProcessManager>()
                .GetCurrentProcessPath()
                .Returns("executablePath");
            
            systemUnderTest.StartWithWindows = true;

            container
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
            container
                .Resolve<IRegistryManager>()
                .GetValue(
                    GetRunRegistryPath(),
                    "Shapeshifter")
                .Returns("somePath");

            systemUnderTest.StartWithWindows = false;

            container
                .Resolve<IRegistryManager>()
                .Received()
                .RemoveValue(
                    GetRunRegistryPath(),
                    "Shapeshifter");
        }
    }
}