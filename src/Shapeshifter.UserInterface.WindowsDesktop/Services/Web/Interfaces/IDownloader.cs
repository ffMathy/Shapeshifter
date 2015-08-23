using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IDownloader : ISingleInstance
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}
