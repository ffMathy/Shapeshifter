namespace Shapeshifter.WindowsDesktop.Services
{
    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    [TestClass]
    public class SettingsManagerTest: TestBase
    {
        [TestMethod]
        public void CanPersistAndFetchStrings()
        {
            var container = CreateContainer();

            var settingsManager = container.Resolve<ISettingsManager>();
            settingsManager.SaveSetting("string", "foobar");

            var setting = settingsManager.LoadSetting<string>("string");
            Assert.AreEqual("foobar", setting);
        }

        [TestMethod]
        public void CanPersistAndFetchIntegers()
        {
            var container = CreateContainer();

            var settingsManager = container.Resolve<ISettingsManager>();
            settingsManager.SaveSetting("integer", 1337);

            var setting = settingsManager.LoadSetting<int>("integer");
            Assert.AreEqual(1337, setting);
        }
    }
}