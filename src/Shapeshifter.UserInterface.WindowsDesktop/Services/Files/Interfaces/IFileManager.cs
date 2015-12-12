namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Infrastructure.Dependencies.Interfaces;

    public interface IFileManager: ISingleInstance, IDisposable
    {
        string WriteBytesToTemporaryFile(string path, byte[] bytes);

        string PrepareTemporaryFolder(string relativePath);

        string PrepareFolder(string relativePath);

        void DeleteDirectoryIfExists(string path);

        void DeleteFileIfExists(string path);

        string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths);
    }
}