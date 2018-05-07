namespace Shapeshifter.WindowsDesktop.Data
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
    public class FormatDataPairTest: TestBase
    {
        [TestMethod]
        public void CanConstructFormatDataPair()
        {
            var item = new FormatDataPair(
				CreateClipboardFormatFromNumber(1337u),
                new byte[]
                {
                    1,
                    2,
                    3
                });
            Assert.AreEqual(1337u, item.Format.Number);
            Assert.AreEqual(1, item.Data[0]);
            Assert.AreEqual(2, item.Data[1]);
            Assert.AreEqual(3, item.Data[2]);
        }
    }
}