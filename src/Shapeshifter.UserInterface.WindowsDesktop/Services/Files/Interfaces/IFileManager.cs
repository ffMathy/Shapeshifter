using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileManager : ISingleInstance
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryPath(string path);
    }
}
