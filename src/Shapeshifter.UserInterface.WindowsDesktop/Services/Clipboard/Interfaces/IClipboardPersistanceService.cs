using System.Collections.Generic;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    public interface IClipboardPersistanceService
    {
        Task PersistClipboardPackageAsync(IClipboardDataPackage package);

        Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync(IClipboardDataPackage package);

        Task DeletePackageAsync(IClipboardDataPackage package);
    }
}
