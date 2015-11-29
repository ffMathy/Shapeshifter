using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardPersistanceService : IClipboardPersistanceService
    {
        public ClipboardPersistanceService(IFileManager fileManager)
        {
            
        }

        public Task PersistClipboardPackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }

        public Task DeletePackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}
