using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileDownloader
    {
        Task DownloadAsync(string fileUrl, string localFileDestination);
    }
}
