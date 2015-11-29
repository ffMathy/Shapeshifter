namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Data.Interfaces;

    public interface IClipboardPersistanceService
    {
        Task PersistClipboardPackageAsync(IClipboardDataPackage package);

        Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync(
            IClipboardDataPackage package);

        Task DeletePackageAsync(IClipboardDataPackage package);
    }
}