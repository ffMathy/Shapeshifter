using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment
{
    [TestClass]
    public class EnvironmentInformationTest
    {
        [TestMethod]
        public void EnvironmentShouldAlwaysUpdate()
        {
            var systemUnderTest = new EnvironmentInformation(false);
            Assert.IsTrue(systemUnderTest.GetShouldUpdate());
        }

        [TestMethod]
        public void EnvironmentShouldNeverBeInDebuggingModePerDefault()
        {
            var systemUnderTest = new EnvironmentInformation(false);
            Assert.IsFalse(systemUnderTest.GetIsDebugging());
        }
    }
}
