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
            systemUnderTest.SaveSetting("string", "foobar");

            var setting = systemUnderTest.LoadSetting<string>("string");
            Assert.AreEqual("foobar", setting);
        }

        [TestMethod]
        public void CanPersistAndFetchIntegers()
        {
            systemUnderTest.SaveSetting("integer", 1337);

            var setting = systemUnderTest.LoadSetting<int>("integer");
            Assert.AreEqual(1337, setting);
        }
    }
}