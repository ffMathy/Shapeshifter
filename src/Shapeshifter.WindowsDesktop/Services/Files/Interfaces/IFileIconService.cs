namespace Shapeshifter.WindowsDesktop.Services.Files.Interfaces
{
    using Infrastructure.Dependencies.Interfaces;

    public interface IFileIconService: ISingleInstance
    {
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}