using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.WindowsDesktop.Services.Interfaces;
using System.Windows.Media;

namespace Shapeshifter.WindowsDesktop.Services
{
    [TestClass]
    public class ColorBrightnessAdjustmentServiceTest: UnitTestFor<IColorBrightnessAdjustmentService>
    {
        [TestMethod]
        public void AdjustingBrightnessRetainsExistingAlphaLevel()
        {
            var adjustedColor = SystemUnderTest.AdjustBrightness(
                new Color()
                {
                    A = 50
                },
                10);
            Assert.AreEqual(50, adjustedColor.A);
        }
    }
}
