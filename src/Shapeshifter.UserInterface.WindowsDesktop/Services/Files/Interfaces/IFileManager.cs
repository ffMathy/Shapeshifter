namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    using Infrastructure.Dependencies.Interfaces;

    public interface IFileManager: ISingleInstance
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareFolder(string path);
    }
}