namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Factories.Interfaces;

    using Files.Interfaces;

    using Interfaces;

    class ClipboardPersistanceService: IClipboardPersistanceService
    {
        readonly IFileManager fileManager;

        readonly IClipboardDataControlPackageFactory factory;

        public ClipboardPersistanceService(
            IFileManager fileManager,
            IClipboardDataControlPackageFactory factory)
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
            var packageFolder = fileManager.PrepareFolder(
                Path.Combine("Pinned", unixTimestamp.ToString()));
            return packageFolder;
        }

        public async Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync()
        {
            var packageList = new List<IClipboardDataPackage>();
            var packageFolder = fileManager.PrepareFolder("Pinned");
            foreach (var directory in Directory.GetDirectories(packageFolder))
            {
                packageList.Add(await GetPersistedPackageAsync(directory));
            }

            return packageList;
        }

        Task<IClipboardDataPackage> GetPersistedPackageAsync(string directory)
        {
            var packageFiles = Directory.GetFiles(directory)
                .OrderBy(x => x);
            
            factory.CreateFromFormatAndRawData()
            package.AddData();
        }

        public Task DeletePackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}