namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;

    class PinClipboardDataAction: IPinClipboardDataAction
    {
        readonly IClipboardPersistenceService clipboardPersistenceService;
        public string Title => "Pin to clipboard";

        public async Task<string> GetDescriptionAsync(IClipboardDataPackage package)
        {
            return "Pins this item so that it always stays in the top, and gets saved to your harddrive so that it is there the next time you open Shapeshifter.";
        }

        public byte Order => byte.MaxValue;

        public PinClipboardDataAction(
            IClipboardPersistenceService clipboardPersistenceService)
        {
            this.clipboardPersistenceService = clipboardPersistenceService;
        }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return GetRelevantData(package)
                .Any();
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            await clipboardPersistenceService.PersistClipboardPackageAsync(package);
        }

        static IEnumerable<IClipboardData> GetRelevantData(IClipboardDataPackage package)
        {
            return package.Contents
                          .Where(x => x.RawData != null);
        }
    }
}