namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IDownloader: ISingleInstance
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}