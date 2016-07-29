namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using WindowsDesktop.Services.Files.Interfaces;

    using Controls.Designer.Services;

    class DesignerFileManager
        : IFileManager,
          IDesignerService
    {
        public void AppendLineToFile(string path, string line)
        {
            
        }

        public string WriteBytesToTemporaryFile(string relativePath, byte[] bytes)
        {
            return null;
        }

        public string AppendLineToTemporaryFile(string relativePath, string line)
        {
            return null;
        }

        public string PrepareTemporaryFolder(string path)
        {
            return null;
        }

        public string PrepareIsolatedFolder(string relativePath = null)
        {
            return null;
        }

        public string GetIsolatedPathRoot(string relativePath)
        {
            return null;
        }

        public string PrepareNewIsolatedFolder(string relativePath)
        {
            return null;
        }

        public string PrepareFolder(string path)
        {
            return null;
        }

        public Task DeleteDirectoryIfExistsAsync(string relativePath)
        {
            return null;
        }

        public Task DeleteFileIfExistsAsync(string relativePath)
        {
            return null;
        }

        public Task DeleteIsolatedFileIfExistsAsync(string path)
        {
            return null;
        }

        public Task DeleteIsolatedDirectoryIfExistsAsync(string path)
        {
            return null;
        }

        public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
        {
            return null;
        }

        public void WriteBytesToFile(string relativePath, byte[] bytes)
        {
        }

        public void Dispose() { }
    }
}