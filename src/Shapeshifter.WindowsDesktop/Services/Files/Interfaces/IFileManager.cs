namespace Shapeshifter.WindowsDesktop.Services.Files.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IFileManager
        : ISingleInstance,
          IDisposable
    {
        string WriteBytesToTemporaryFile(
            string path,
            byte[] bytes);

        string PrepareTemporaryFolder(
            string relativePath);

        string PrepareIsolatedFolder(
            string relativePath = null);

        string PrepareNewIsolatedFolder(
            string relativePath);

        string PrepareFolder(
            string path);

        Task DeleteDirectoryIfExistsAsync(
            string relativePath);

        Task DeleteFileIfExistsAsync(
            string relativePath);

        Task DeleteIsolatedFileIfExistsAsync(
            string path);

        Task DeleteIsolatedDirectoryIfExistsAsync(
            string path);

        string FindCommonFolderFromPaths(
            IReadOnlyCollection<string> paths);
    }
}