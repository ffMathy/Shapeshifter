using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Net.Http;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class Downloader : IDownloader, IDisposable
    {
        readonly HttpClient client;

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
