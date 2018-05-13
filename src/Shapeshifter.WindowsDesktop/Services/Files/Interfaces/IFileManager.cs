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
        Task WriteBytesToFileAsync(
            string path,
            byte[] bytes);

        Task AppendLineToFileAsync(
            string path,
            string line);

        Task<string> WriteBytesToTemporaryFileAsync(
            string relativePath,
            byte[] bytes);

        Task<string> AppendLineToTemporaryFileAsync(
            string relativePath,
            string line);

        Task<string> PrepareTemporaryFolderAsync(
            string relativePath);

        string PrepareIsolatedFolder(
            string relativePath = null);

        string PrepareFolder(
            string path);

		Task CopyFileAsync(
			string origin, 
			string destination);

        Task DeleteDirectoryIfExistsAsync(
            string path);

        Task DeleteFileIfExistsAsync(
            string path);

        Task DeleteIsolatedFileIfExistsAsync(
            string relativePath);

        Task DeleteIsolatedDirectoryIfExistsAsync(
            string relativePath);

        string FindCommonFolderFromPaths(
            IReadOnlyCollection<string> paths);
    }
}