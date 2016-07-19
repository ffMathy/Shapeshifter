namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Data.Interfaces;

    public interface IClipboardPersistanceService
    {
        Task PersistClipboardPackageAsync(IClipboardDataPackage package);

        Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync();

        Task DeletePackageAsync(IClipboardDataPackage package);

        Task IsPinnedAsync(IClipboardDataPackage package);
    }
}