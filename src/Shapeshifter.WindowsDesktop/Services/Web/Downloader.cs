namespace Shapeshifter.WindowsDesktop.Services.Web
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Interfaces;

    
    class Downloader
        : IDownloader,
          IDisposable
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