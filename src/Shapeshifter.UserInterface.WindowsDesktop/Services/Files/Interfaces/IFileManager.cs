namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    using System.Collections.Generic;

    using Infrastructure.Dependencies.Interfaces;

    public interface IFileManager: ISingleInstance
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryFolder(string path);

        string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths);
    }
}