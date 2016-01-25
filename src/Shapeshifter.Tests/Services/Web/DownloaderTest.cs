namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System.Threading.Tasks;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DownloaderTest: UnitTestFor<IDownloader>
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanDownloadGoogle()
        {
            using (SystemUnderTest)
            {
                var bytes = await SystemUnderTest.DownloadBytesAsync(
                    "http://google.com");
                Assert.AreNotEqual(0, bytes.Length);
            }
        }
    }
}