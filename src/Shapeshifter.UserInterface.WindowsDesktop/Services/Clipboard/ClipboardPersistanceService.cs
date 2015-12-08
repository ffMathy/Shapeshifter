namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Files.Interfaces;

    using Interfaces;

    using Structures;

    class ClipboardPersistanceService: IClipboardPersistanceService
    {
        readonly IFileManager fileManager;

        readonly IClipboardDataPackageFactory factory;

        public ClipboardPersistanceService(
            IFileManager fileManager,
            IClipboardDataPackageFactory factory)
        {
            this.fileManager = fileManager;
            this.factory = factory;
        }

        public async Task PersistClipboardPackageAsync(IClipboardDataPackage package)
        {
            var packageFolder = PrepareUniquePackageFolder();
            for (var i = 1; i <= package.Contents.Count; i++)
            {
                var content = package.Contents[i];
                var filePath = Path.Combine(packageFolder, i.ToString());
                fileManager.WriteBytesToTemporaryFile(filePath, content.RawData);
            }
        }

        string PrepareUniquePackageFolder()
        {
            var unixTimestamp = DateTime.UtcNow.ToFileTime();
            var packageFolder = fileManager.PrepareTemporaryFolder(
                Path.Combine("Pinned", unixTimestamp.ToString()));
            return packageFolder;
        }

        public async Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync()
        {
            var packageList = new List<IClipboardDataPackage>();
            var packageFolder = fileManager.PrepareTemporaryFolder("Pinned");
            foreach (var directory in Directory.GetDirectories(packageFolder))
            {
                packageList.Add(await GetPersistedPackageAsync(directory));
            }

            return packageList;
        }

        async Task<IClipboardDataPackage> GetPersistedPackageAsync(string directory)
        {
            var packageFiles = Directory.GetFiles(directory).OrderBy(x => x);

            var dataPairs = new List<FormatDataPair>();
            foreach (var file in packageFiles)
            {
                var fileExtension = Path.GetExtension(file);
                Debug.Assert(fileExtension != null, "fileExtension != null");

                var fileExtensionWithoutDot = fileExtension.Substring(1);

                var format = uint.Parse(fileExtensionWithoutDot);
                var data = File.ReadAllBytes(file);

                dataPairs.Add(new FormatDataPair(format, data));
            }

            return factory.CreateFromFormatsAndData(dataPairs.ToArray());
        }

        public Task DeletePackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}