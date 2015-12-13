namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
    using Services.Files.Interfaces;

    class ZipFilesAction: IZipFilesAction
    {
        readonly IAsyncFilter asyncFilter;

        readonly IFileManager fileManager;

        readonly IClipboardInjectionService clipboardInjectionService;

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

        async Task<IReadOnlyCollection<IClipboardData>> GetSupportedData(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData;
        }

        static async Task<bool> CanPerformAsync(
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
            var firstSupportedData = supportedDataCollection.FirstOrDefault();

            var zipFilePath = ZipData(firstSupportedData);
            clipboardInjectionService.InjectFiles(zipFilePath);
        }

        string ZipFileCollectionData(params IClipboardFileData[] fileDataItems)
        {
            if (fileDataItems.Length == 0)
            {
                throw new ArgumentException(
                    "There must be at least one item to compress.",
                    nameof(fileDataItems));
            }

            var filePaths = fileDataItems
                .Select(x => x.FullPath)
                .ToArray();
            var commonPath = fileManager.FindCommonFolderFromPaths(filePaths);
            var directoryName = Path.GetFileName(commonPath);
            var directoryPath = fileManager.PrepareTemporaryFolder(directoryName);
            CopyFilesToTemporaryFolder(fileDataItems, directoryPath);

            var zipFile = ZipDirectory(directoryPath);
            return zipFile;
        }

        void CopyFilesToTemporaryFolder(
            IEnumerable<IClipboardFileData> fileDataItems,
            string directory)
        {
            foreach (var fileData in fileDataItems)
            {
                CopyFileToTemporaryFolder(directory, fileData);
            }
        }

        void CopyFileToTemporaryFolder(string directory, IClipboardFileData fileData)
        {
            var destinationFilePath = Path.Combine(
                directory, fileData.FileName);
            fileManager.DeleteFileIfExists(destinationFilePath);
            File.Copy(fileData.FullPath, destinationFilePath);
        }

        string ZipDirectory(string directory)
        {
            var directoryName = Path.GetFileName(directory);
            var compressedFolderDirectory = fileManager.PrepareTemporaryFolder($"Compressed folders");
            var zipFile = Path.Combine(compressedFolderDirectory, $"{directoryName}.zip");

            fileManager.DeleteFileIfExists(zipFile);
            ZipFile.CreateFromDirectory(directory, zipFile);

            return zipFile;
        }

        string ZipData(IClipboardData data)
        {
            var clipboardFileData = data as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return ZipFileCollectionData(clipboardFileData);
            }

            var clipboardFileCollectionData = data as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return ZipFileCollectionData(
                    clipboardFileCollectionData
                        .Files
                        .ToArray());
            }

            throw new InvalidOperationException("Unknown data format.");
        }
    }
}