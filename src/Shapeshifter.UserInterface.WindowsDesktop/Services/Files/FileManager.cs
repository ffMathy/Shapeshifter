namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Files
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class FileManager
        : IFileManager
    {
        readonly ICollection<string> temporaryPaths;

        public FileManager()
        {
            temporaryPaths = new HashSet<string>();
            ClearDirectory();
        }

        static void ClearDirectory()
        {
            var directory = PrepareIsolatedTemporaryFolder();
            Directory.Delete(directory, true);
        }

        public void Dispose()
        {
            foreach (var temporaryPath in temporaryPaths)
            {
                PurgePath(temporaryPath);
            }
        }

        void PurgePath(string temporaryPath)
        {
            DeleteFileIfExists(temporaryPath);
            DeleteDirectoryIfExists(temporaryPath);
        }

        public void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }

        static string PrepareIsolatedFolder()
        {
            return PrepareIsolatedFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        static string PrepareIsolatedFolder(string basePath)
        {
            const string folderName = "Shapeshifter";
            
            var path = Path.Combine(basePath, folderName);
            CreateDirectoryIfNotExists(path);

            return path;
        }

        static string PrepareIsolatedTemporaryFolder()
        {
            return PrepareIsolatedFolder(Path.GetTempPath());
        }

        public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
        {
            var pathSimilarityIndex = GetPathSegmentsInCommonCount(paths);

            var firstPath = paths.First();
            var segments = GetPathSegments(firstPath);

            var commonPath = Path.Combine(
                segments
                    .Take(pathSimilarityIndex)
                    .ToArray());

            return commonPath;
        }

        static int GetPathSegmentsInCommonCount(IReadOnlyCollection<string> paths)
        {
            var commonIndex = 0;
            foreach (var originPath in paths)
            {
                if (FindPathCommonIndexAmongPaths(paths, originPath, ref commonIndex))
                {
                    return commonIndex;
                }
            }

            return commonIndex;
        }

        static bool FindPathCommonIndexAmongPaths(IReadOnlyCollection<string> paths, string path, ref int commonIndex)
        {
            var originSegments = GetPathSegments(path);
            for (var index = 0; index < originSegments.Length; index++)
            {
                var originSegment = originSegments[index];
                foreach (var referencePath in paths)
                {
                    var referenceSegments = GetPathSegments(referencePath);
                    if (referenceSegments.Length < originSegments.Length)
                    {
                        return true;
                    }

                    var referenceSegment = referenceSegments[index];
                    if (originSegment != referenceSegment)
                    {
                        return true;
                    }
                }
                commonIndex++;
            }
            return false;
        }

        static string[] GetPathSegments(string originPath)
        {
            return originPath.Split('\\', '/');
        }

        public string PrepareFolder(string relativePath)
        {
            var finalPath = GetFullPathFromRelativePath(relativePath);
            CreateDirectoryIfNotExists(finalPath);
            return finalPath;
        }

        public string PrepareTemporaryFolder(string relativePath)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(relativePath);
            WatchDirectory(finalPath);

            return finalPath;
        }

        void WatchDirectory(string finalPath)
        {
            temporaryPaths.Add(finalPath);
            CreateDirectoryIfNotExists(finalPath);
        }

        static void CreateDirectoryIfNotExists(string relativePath)
        {
            if (!Directory.Exists(relativePath))
            {
                Directory.CreateDirectory(relativePath);
            }
        }

        static string GetFullPathFromRelativePath(string path)
        {
            var isolatedFolderPath = PrepareIsolatedFolder();

            var finalPath = Path.Combine(isolatedFolderPath, path);
            return finalPath;
        }

        static string GetFullPathFromRelativeTemporaryPath(string path)
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