using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IDownloader
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}
