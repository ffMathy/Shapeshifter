using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Net.Http;
using System.IO;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class FileDownloader : IFileDownloader
    {
        private readonly HttpClient client;

        public FileDownloader()
        {
            client = new HttpClient();
        }

        public async Task DownloadAsync(string fileUrl, string localFileDestination)
        {
            var bytes = await client.GetByteArrayAsync(fileUrl);
            File.WriteAllBytes(localFileDestination, bytes);
        }
    }
}
