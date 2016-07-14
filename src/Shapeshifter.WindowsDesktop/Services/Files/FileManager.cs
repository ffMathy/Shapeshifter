namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure.Threading;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    class FileManager
        : IFileManager
    {
        readonly IRetryingThreadLoop retryingThreadLoop;

        readonly ICollection<string> temporaryPaths;

        public FileManager(
            IRetryingThreadLoop retryingThreadLoop)
        {
            this.retryingThreadLoop = retryingThreadLoop;

            temporaryPaths = new HashSet<string>();

            PurgeTemporaryDirectory();
        }

        void PurgeTemporaryDirectory()
        {
            var directory = PrepareTemporaryFolder();
            DeleteIsolatedDirectoryIfExistsAsync(directory);
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
            DeleteFileIfExists(temporaryPath);
            DeleteDirectoryIfExists(temporaryPath);
        }

        static RetryingThreadLoopJob CreateRetryingFileJob(
            Func<Task> task)
        {
            return new RetryingThreadLoopJob
            {
                Action = task,
                AttemptsBeforeFailing = 5,
                IntervalInMilliseconds = 1000,
                IsExceptionIgnored = IsExceptionIgnored
            };
        }

        static bool IsExceptionIgnored(Exception ex)
        {
            return ex is IOException;
        }

        public Task DeleteFileIfExistsAsync(string path)
        {
            return retryingThreadLoop.StartAsync(
                CreateRetryingFileJob(
                    async () => DeleteFileIfExists(path)));
        }

        static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public Task DeleteIsolatedFileIfExistsAsync(string path)
        {
            return DeleteFileIfExistsAsync(
                GetFullPathFromIsolatedPath(path));
        }

        public Task DeleteIsolatedDirectoryIfExistsAsync(string path)
        {
            return DeleteDirectoryIfExistsAsync(
                GetFullPathFromIsolatedPath(path));
        }

        public Task DeleteDirectoryIfExistsAsync(string path)
        {
            return retryingThreadLoop.StartAsync(
                CreateRetryingFileJob(
                    async () => DeleteDirectoryIfExists(path)));
        }

        static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        static string GetIsolatedPathRoot()
        {
            return PrepareShapeshifterFolder(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData));
        }

        static string PrepareShapeshifterFolder(string basePath)
        {
            const string folderName = "Shapeshifter";

            var path = Path.Combine(basePath, folderName);
            CreateDirectoryIfNotExists(path);

            return path;
        }

        static string PrepareTemporaryFolder()
        {
            return PrepareShapeshifterFolder(
                Path.GetTempPath());
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
            var splitPaths = paths
                .Select(p => p.Split('\\', '/'))
                .ToList();

            var shortestPath = splitPaths
                .OrderBy(p => p.Count())
                .First();

            var commonCount = shortestPath
                .TakeWhile((s, i) =>
                    splitPaths.All(sp => sp[i] == s))
                .Count();

            return commonCount;
        }

        static string[] GetPathSegments(string originPath)
        {
            return originPath.Split('\\', '/');
        }

        public string PrepareNewIsolatedFolder(string relativePath)
        {
            var count = 0;

            string finalPath = null;
            while ((finalPath == null) || Directory.Exists(finalPath))
            {
                finalPath = GetFullPathFromIsolatedPath(
                    Path.Combine(relativePath, (++count).ToString()));
            }

            return PrepareFolder(finalPath);
        }

        public string PrepareFolder(string path)
        {
            CreateDirectoryIfNotExists(path);
            return path;
        }

        public string PrepareIsolatedFolder(string relativePath = null)
        {
            var finalPath = GetFullPathFromIsolatedPath(relativePath);
            return PrepareFolder(finalPath);
        }

        public string WriteBytesToTemporaryFile(string relativePath, byte[] bytes)
        {
            var finalPath = GetFullPathFromTemporaryPath(relativePath);
            WriteBytesToFile(finalPath, bytes);

            temporaryPaths.Add(finalPath);

            return finalPath;
        }

        public string PrepareTemporaryFolder(string relativePath)
        {
            var finalPath = GetFullPathFromTemporaryPath(relativePath);
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

        static string GetFullPathFromIsolatedPath(string path = null)
        {
            var isolatedFolderPath = GetIsolatedPathRoot();

            var finalPath = path == null
                                ? isolatedFolderPath
                                : Path.Combine(isolatedFolderPath, path);
            return finalPath;
        }

        static string GetFullPathFromTemporaryPath(string path)
        {
            var isolatedFolderPath = PrepareTemporaryFolder();

            var finalPath = Path.Combine(isolatedFolderPath, path);
            return finalPath;
        }

        public void WriteBytesToFile(string relativePath, byte[] bytes)
        {
            File.WriteAllBytes(relativePath, bytes);
        }
    }
}