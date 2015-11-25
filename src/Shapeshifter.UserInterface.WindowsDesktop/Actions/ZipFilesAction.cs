using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class ZipFilesAction : IZipFilesAction
    {
        private readonly IAsyncFilter asyncFilter;
        private readonly IFileManager fileManager;
        private readonly IClipboardInjectionService clipboardInjectionService;

        public ZipFilesAction(
            IAsyncFilter asyncFilter,
            IFileManager fileManager,
            IClipboardInjectionService clipboardInjectionService)
        {
            this.asyncFilter = asyncFilter;
            this.fileManager = fileManager;
            this.clipboardInjectionService = clipboardInjectionService;
        }

        public string Description => "Compress the clipboard contents into a ZIP-file and copy it.";

        public byte Order => 75;

        public string Title => "Copy as compressed folder";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await GetSupportedData(package);
            return supportedData.Any();
        }

        private async Task<IReadOnlyCollection<IClipboardData>> GetSupportedData(IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData;
        }

        private static async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return
                data is IClipboardFileData ||
                data is IClipboardFileCollectionData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage processedData)
        {
            var supportedDataCollection = await GetSupportedData(processedData);
            var firstSupportedData = supportedDataCollection.First();

            var zipFilePath = ZipData(firstSupportedData);
            clipboardInjectionService.InjectFiles(zipFilePath);
        }

        private string ZipFileCollectionData(params IClipboardFileData[] fileDataItems)
        {
            if (fileDataItems == null)
            {
                throw new ArgumentNullException(nameof(fileDataItems));
            }

            if (fileDataItems.Length == 0)
            {
                throw new ArgumentException("There must be at least one item to compress.", nameof(fileDataItems));
            }

            var filePaths = fileDataItems
                .Select(x => x.FullPath)
                .ToArray();
            var commonPath = FindCommonFolder(filePaths);
            var directoryName = Path.GetFileName(commonPath);
            var directoryPath = fileManager.PrepareFolder(directoryName);
            CopyFilesToTemporaryFolder(fileDataItems, directoryPath);

            var zipFile = ZipDirectory(directoryPath);
            return zipFile;
        }

        private static string FindCommonFolder(IReadOnlyCollection<string> paths)
        {
            var pathSimilarityIndex = GetPathSegmentsInCommonCount(paths);

            var firstPath = paths.First();
            var segments = GetPathSegments(firstPath);

            var commonPath = Path.Combine(segments
                    .Take(pathSimilarityIndex)
                    .ToArray());

            return commonPath;
        }

        private static int GetPathSegmentsInCommonCount(IReadOnlyCollection<string> paths)
        {
            var commonIndex = 0;
            foreach (var originPath in paths)
            {
                var originSegments = GetPathSegments(originPath);
                for (var index = 0; index < originSegments.Length; index++)
                {
                    var originSegment = originSegments[index];
                    foreach (var referencePath in paths)
                    {
                        var referenceSegments = GetPathSegments(referencePath);
                        if (referenceSegments.Length < originSegments.Length)
                        {
                            return commonIndex;
                        }

                        var referenceSegment = referenceSegments[index];
                        if (originSegment != referenceSegment)
                        {
                            return commonIndex;
                        }
                    }
                    commonIndex++;
                }
            }

            return commonIndex;
        }

        private static string[] GetPathSegments(string originPath)
        {
            return originPath.Split('\\', '/');
        }

        private static void CopyFilesToTemporaryFolder(IEnumerable<IClipboardFileData> fileDataItems, string directory)
        {
            foreach (var fileData in fileDataItems)
            {
                var destinationFilePath = Path.Combine(directory, fileData.FileName);
                File.Copy(fileData.FullPath, destinationFilePath);
            }
        }

        private string ZipDirectory(string directory)
        {
            var directoryName = Path.GetFileName(directory);
            var zipFile = fileManager.PrepareFolder($"{directoryName}.zip");

            ZipFile.CreateFromDirectory(directory, zipFile);

            return zipFile;
        }

        private string ZipData(IClipboardData data)
        {
            var clipboardFileData = data as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return ZipFileCollectionData(clipboardFileData);
            }

            var clipboardFileCollectionData = data as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return ZipFileCollectionData(clipboardFileCollectionData
                    .Files
                    .ToArray());
            }

            throw new InvalidOperationException("Unknown data format.");
        }
    }
}