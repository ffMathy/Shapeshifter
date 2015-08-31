using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IFileManager : ISingleInstance
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryPath(string path);
    }
}
