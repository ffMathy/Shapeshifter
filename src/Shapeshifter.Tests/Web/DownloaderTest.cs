namespace Shapeshifter.WindowsDesktop.Web
{
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shared.Services.Web.Interfaces;

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