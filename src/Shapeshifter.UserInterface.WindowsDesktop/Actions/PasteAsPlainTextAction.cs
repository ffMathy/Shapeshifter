using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class PasteAsPlainTextAction : IPasteAsPlainTextAction
    {
        private readonly IClipboardInjectionService clipboardInjectionService;
        private readonly IAsyncFilter asyncFilter;

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

        private async Task<IClipboardData> GetFirstSupportedItem(IClipboardDataPackage data)
        {
            var supportedData = await asyncFilter.FilterAsync(data.Contents, CanPerformAsync);
            return supportedData.FirstOrDefault();
        }

        private static async Task<bool> CanPerformAsync(
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