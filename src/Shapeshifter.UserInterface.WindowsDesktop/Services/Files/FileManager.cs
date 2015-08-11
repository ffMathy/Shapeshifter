using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    class FileManager : IFileManager, IDisposable
    {
        private readonly ICollection<string> temporaryPaths;

        public FileManager()
        {
            temporaryPaths = new HashSet<string>();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private string PrepareIsolatedTemporaryFolder()
        {
            const string folderName = "Shapeshifter";

            var temporaryDirectory = Path.GetTempPath();
            var path = Path.Combine(temporaryDirectory, folderName);

            PrepareDirectory(path);

            return path;
        }

        private static void PrepareDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string PrepareTemporaryPath(string path)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(path);
            temporaryPaths.Add(finalPath);

            if (IsDirectory(finalPath))
            {
                PrepareDirectory(finalPath);
            }

            return finalPath;
        }

        private static bool IsDirectory(string finalPath)
        {
            var fileNameWithExtension = Path.GetFileName(finalPath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(finalPath);
            return fileNameWithExtension == fileNameWithoutExtension;
        }

        private string GetFullPathFromRelativeTemporaryPath(string path)
        {
            var isolatedFolderPath = PrepareIsolatedTemporaryFolder();

            var finalPath = Path.Combine(isolatedFolderPath, path);
            return finalPath;
        }

        public void WriteBytesToTemporaryFile(string fileName, byte[] bytes)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(fileName);
            temporaryPaths.Add(finalPath);

            File.WriteAllBytes(fileName, bytes);
        }
    }
}
