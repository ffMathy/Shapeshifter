namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Files.Interfaces;

    using Interfaces;

    class ClipboardPersistanceService: IClipboardPersistanceService
    {
        readonly IFileManager fileManager;

        public ClipboardPersistanceService(
            IFileManager fileManager)
        {
            this.fileManager = fileManager;
        }

        public async Task PersistClipboardPackageAsync(IClipboardDataPackage package)
        {
            var unixTimestamp = DateTime.UtcNow.ToFileTime();
            var packageFolder = fileManager.PrepareFolder(
                                                          Path.Combine(
                                                                       "Pinned",
                                                                       unixTimestamp.ToString()));
            for (var i = 1; i <= package.Contents.Count; i++)
            {
                var content = package.Contents[i];
                var filePath = Path.Combine(packageFolder, i.ToString());
                fileManager.WriteBytesToTemporaryFile(filePath, content.RawData);
            }
        }

        public Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync(
            IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }

        public Task DeletePackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}