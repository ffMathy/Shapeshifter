namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System.Threading.Tasks;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DownloaderTest: TestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanDownloadGoogle()
        {
            var container = CreateContainer();

            using (var downloader = container.Resolve<IDownloader>())
            {
                var bytes = await downloader.DownloadBytesAsync("http://google.com");
                Assert.AreNotEqual(0, bytes.Length);
            }
        }
    }
}