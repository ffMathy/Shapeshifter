namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileIconService
    {
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}
