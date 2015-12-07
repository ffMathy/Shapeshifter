namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;

    class PasteAsPlainTextAction: IPasteAsPlainTextAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;

        readonly IAsyncFilter asyncFilter;

        public PasteAsPlainTextAction(
            IClipboardInjectionService clipboardInjectionService,
            IAsyncFilter asyncFilter)
        {
            this.clipboardInjectionService = clipboardInjectionService;
            this.asyncFilter = asyncFilter;
        }

        public string Description => "Pastes clipboard contents as plain text.";

        public byte Order => 25;

        public string Title => "Paste as plain text";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage data)
        {
            return await GetFirstSupportedItem(data) != null;
        }

        async Task<IClipboardData> GetFirstSupportedItem(IClipboardDataPackage data)
        {
            var supportedData = await asyncFilter.FilterAsync(data.Contents, CanPerformAsync);
            return supportedData.FirstOrDefault();
        }

        static async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return data is IClipboardTextData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedItem(package);
            clipboardInjectionService.InjectText(textData.Text);
        }
    }
}