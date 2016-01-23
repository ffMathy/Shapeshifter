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
            using (systemUnderTest)
            {
                var bytes = await systemUnderTest.DownloadBytesAsync(
                    "http://google.com");
                Assert.AreNotEqual(0, bytes.Length);
            }
        }
    }
}