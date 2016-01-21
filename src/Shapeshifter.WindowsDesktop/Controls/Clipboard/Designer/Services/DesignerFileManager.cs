namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Collections.Generic;

    using WindowsDesktop.Services.Files.Interfaces;

    using Controls.Designer.Services;

    class DesignerFileManager
        : IFileManager,
          IDesignerService
    {
        public string PrepareTemporaryFolder(string path)
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

        public void DeleteDirectoryIfExistsAsync(string path) { }

        public void DeleteFileIfExistsAsync(string path) { }

        public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
        {
            return null;
        }

        public string WriteBytesToTemporaryFile(string path, byte[] bytes)
        {
            return null;
        }

        public void Dispose() { }
    }
}