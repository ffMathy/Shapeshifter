using System;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class PasteAction : IPasteAction
    {
        private readonly IClipboardInjectionService clipboardInjectionService;
        private readonly IClipboardPasteService clipboardPasteService;
        private readonly ILogger logger;

        public PasteAction(
            IClipboardInjectionService clipboardInjectionService,
            IClipboardPasteService clipboardPasteService,
            ILogger logger)
        {
            this.clipboardInjectionService = clipboardInjectionService;
            this.clipboardPasteService = clipboardPasteService;
            this.logger = logger;
        }

        public string Description => "Pastes clipboard contents as-is.";

        public byte Order => 0;

        public string Title => "Paste";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            return true;
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            clipboardInjectionService.InjectData(package);
            clipboardPasteService.PasteClipboardContents();

            logger.Information("Paste action performed.", 1);
        }
    }
}