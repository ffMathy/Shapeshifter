using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IDownloader
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}
