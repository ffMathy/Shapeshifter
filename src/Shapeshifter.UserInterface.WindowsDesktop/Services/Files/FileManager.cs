using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    [ExcludeFromCodeCoverage]
    internal class FileManager : IFileManager, IDisposable
    {
        private readonly ICollection<string> temporaryPaths;

        public FileManager()
        {
            temporaryPaths = new HashSet<string>();
        }

        public void Dispose()
        {
            foreach (var temporaryPath in temporaryPaths)
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
                else if (Directory.Exists(temporaryPath))
                {
                    Directory.Delete(temporaryPath);
                }
            }
        }

        private static string PrepareIsolatedTemporaryFolder()
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

        public string PrepareFolder(string path)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(path);
            temporaryPaths.Add(finalPath);

            PrepareDirectory(path);

            return finalPath;
        }

        private static string GetFullPathFromRelativeTemporaryPath(string path)
        {
            var isolatedFolderPath = PrepareIsolatedTemporaryFolder();

            var finalPath = Path.Combine(isolatedFolderPath, path);
            return finalPath;
        }

        public string WriteBytesToTemporaryFile(string fileName, byte[] bytes)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(fileName);
            temporaryPaths.Add(finalPath);

            File.WriteAllBytes(fileName, bytes);

            return finalPath;
        }
    }
}