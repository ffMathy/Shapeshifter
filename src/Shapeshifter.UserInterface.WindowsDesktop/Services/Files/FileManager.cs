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
        : IFileManager,
          IDisposable
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

        static void PurgePath(string temporaryPath)
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

        static string PrepareIsolatedTemporaryFolder()
        {
            const string folderName = "Shapeshifter";

            var temporaryDirectory = Path.GetTempPath();
            var path = Path.Combine(temporaryDirectory, folderName);

            PrepareDirectory(path);

            return path;
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

        static void PrepareDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string PrepareTemporaryFolder(string path)
        {
            var finalPath = GetFullPathFromRelativeTemporaryPath(path);
            temporaryPaths.Add(finalPath);

            PrepareDirectory(finalPath);

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