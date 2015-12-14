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
        readonly IClipboardPersistanceService clipboardPersistanceService;

        public string Title => "Pin to clipboard";

        public string Description => "Pastes clipboard contents as plain text.";

        public byte Order => 200;

        public PinClipboardDataAction(
            IClipboardPersistanceService clipboardPersistanceService)
        {
            this.clipboardPersistanceService = clipboardPersistanceService;
        }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return GetRelevantData(package).Any();
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            foreach (var item in GetRelevantData(package))
            {
                
            }
        }

        static IEnumerable<IClipboardData> GetRelevantData(IClipboardDataPackage package)
        {
            return package.Contents
                          .Where(x => x.RawData != null);
        }
    }
}