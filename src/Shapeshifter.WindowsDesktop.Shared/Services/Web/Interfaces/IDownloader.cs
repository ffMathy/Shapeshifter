namespace Shapeshifter.WindowsDesktop.Shared.Services.Web.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IDownloader
        : ISingleInstance,
          IDisposable
    {
        Task<byte[]> DownloadBytesAsync(string url);
    }
}