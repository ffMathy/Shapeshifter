namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    using System;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;

    class PasteAction: IPasteAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;

        readonly IClipboardPasteService clipboardPasteService;

        readonly ILogger logger;

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