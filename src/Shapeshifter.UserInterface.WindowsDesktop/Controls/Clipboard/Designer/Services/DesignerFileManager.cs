namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using WindowsDesktop.Services.Files.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
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

        public void DeleteDirectoryIfExists(string path)
        {
        }

        public void DeleteFileIfExists(string path)
        {
        }

        public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
        {
            return null;
        }

        public string WriteBytesToTemporaryFile(string path, byte[] bytes)
        {
            return null;
        }
    }
}