using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
{
    [ExcludeFromCodeCoverage]
    internal class Downloader : IDownloader, IDisposable
    {
        private readonly HttpClient client;

        public Downloader()
        {
            client = new HttpClient();
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            var bytes = await client.GetByteArrayAsync(url);
            return bytes;
        }
    }
}