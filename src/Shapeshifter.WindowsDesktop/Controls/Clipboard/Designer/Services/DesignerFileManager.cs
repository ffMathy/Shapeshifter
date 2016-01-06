namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Collections.Generic;

    using Interfaces;

    using Shared.Services.Files.Interfaces;

    class DesignerFileManager
        : IFileManager,
          IDesignerService
    {
        public string PrepareTemporaryFolder(string path)
        {
            return null;
        }

        public string PrepareFolder(string relativePath)
        {
            return null;
        }

        public string PrepareNewFolder(string relativePath)
        {
            return null;
        }

        public void DeleteDirectoryIfExists(string path) { }

        public void DeleteFileIfExists(string path) { }

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