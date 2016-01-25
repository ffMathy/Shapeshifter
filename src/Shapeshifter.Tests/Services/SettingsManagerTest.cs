namespace Shapeshifter.WindowsDesktop.Services
{
    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SettingsManagerTest: UnitTestFor<ISettingsManager>
    {
        [TestMethod]
        public void CanPersistAndFetchStrings()
        {
            SystemUnderTest.SaveSetting("string", "foobar");

            var setting = SystemUnderTest.LoadSetting<string>("string");
            Assert.AreEqual("foobar", setting);
        }

        [TestMethod]
        public void CanPersistAndFetchIntegers()
        {
            SystemUnderTest.SaveSetting("integer", 1337);

            var setting = SystemUnderTest.LoadSetting<int>("integer");
            Assert.AreEqual(1337, setting);
        }
    }
}