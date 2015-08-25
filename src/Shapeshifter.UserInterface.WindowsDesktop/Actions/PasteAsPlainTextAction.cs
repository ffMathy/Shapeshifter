using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAsPlainTextAction : IPasteAsPlainTextAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;

        public PasteAsPlainTextAction(IClipboardInjectionService clipboardInjectionService)
        {
            this.clipboardInjectionService = clipboardInjectionService;
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

        public async Task<bool> CanPerformAsync(IClipboardData clipboardData)
        {
            return clipboardData is IClipboardTextData;
        }

        public async Task PerformAsync(
            IClipboardData processedData)
        {
            var textData = (IClipboardTextData)processedData;
            clipboardInjectionService.InjectText(textData.Text);
        }
    }
}
