using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

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

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            return package is IClipboardTextData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData)package;
            clipboardInjectionService.InjectText(textData.Text);
        }
    }
}
