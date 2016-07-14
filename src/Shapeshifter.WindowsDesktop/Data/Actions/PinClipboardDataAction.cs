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
        public string Title => "Pin to clipboard";

        public string Description => "Pins this item so that it always stays in the top, and gets saved to your harddrive.";

        public byte Order => byte.MaxValue;

        public PinClipboardDataAction()
        { }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return GetRelevantData(package)
                .Any();
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            foreach (var item in GetRelevantData(package)) { }
        }

        static IEnumerable<IClipboardData> GetRelevantData(IClipboardDataPackage package)
        {
            return package.Contents
                          .Where(x => x.RawData != null);
        }
    }
}