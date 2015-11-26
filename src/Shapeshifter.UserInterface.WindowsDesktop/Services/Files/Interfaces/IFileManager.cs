using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    public interface IFileManager : ISingleInstance
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareFolder(string path);
    }
}