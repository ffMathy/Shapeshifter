namespace Shapeshifter.WindowsDesktop.Structures
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FormatDataPairTest
    {
        [TestMethod]
        public void CanConstructFormatDataPair()
        {
            var item = new FormatDataPair(
                1337,
                new byte[]
                {
                    1,
                    2,
                    3
                });
            Assert.AreEqual(1337u, item.Format);
            Assert.AreEqual(1, item.Data[0]);
            Assert.AreEqual(2, item.Data[1]);
            Assert.AreEqual(3, item.Data[2]);
        }
    }
}