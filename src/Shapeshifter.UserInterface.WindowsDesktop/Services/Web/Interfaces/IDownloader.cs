using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces
{
    public interface IDownloader : ISingleInstance
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}
