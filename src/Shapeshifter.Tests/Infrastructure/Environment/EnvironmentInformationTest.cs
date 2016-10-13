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
    public class EnvironmentInformationTest: UnitTestFor<IEnvironmentInformation>
    {
        [TestMethod]
        public void EnvironmentShouldAlwaysUpdate()
        {
            Assert.IsTrue(SystemUnderTest.GetShouldUpdate());
        }

        [TestMethod]
        public void EnvironmentShouldNeverBeInDebuggingModePerDefault()
        {
            Assert.IsFalse(SystemUnderTest.GetIsDebugging());
        }
    }
}
