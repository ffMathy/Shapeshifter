using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
