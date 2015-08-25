using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAction : IPasteAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;
        readonly IClipboardPasteService clipboardPasteService;

        public PasteAction(
            IClipboardInjectionService clipboardInjectionService,
            IClipboardPasteService clipboardPasteService)
        {
            this.clipboardInjectionService = clipboardInjectionService;
            this.clipboardPasteService = clipboardPasteService;
        }

        public string Description
        {
            get
            {
                return "Pastes clipboard contents as-is.";
            }
        }

        public byte Order
        {
            get
            {
                return 0;
            }
        }

        public string Title
        {
            get
            {
                return "Paste";
            }
        }

        public async Task<bool> CanPerformAsync(IClipboardData clipboardData)
        {
            if(clipboardData == null)
            {
                throw new ArgumentNullException(nameof(clipboardData));
            }

            return true;
        }

        public async Task PerformAsync(
            IClipboardData processedData)
        {
            clipboardInjectionService.InjectData(processedData.RawData);
            clipboardPasteService.PasteClipboardContents();
        }
    }
}
