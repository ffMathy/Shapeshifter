using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.Core.Data;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAsPlainTextAction : IPasteAsPlainTextAction
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

        public string Description
        {
            get
            {
                return "Pastes clipboard contents as plain text.";
            }
        }

        public byte Order
        {
            get
            {
                return 25;
            }
        }

        public string Title
        {
            get
            {
                return "Paste as plain text";
            }
        }

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage data)
        {
            var supportedData = await asyncFilter.FilterAsync(data.Contents, CanPerformAsync);
            return supportedData.Any();
        }

        async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return data is IClipboardTextData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData)package;
            clipboardInjectionService.InjectText(textData.Text);
        }
    }
}
