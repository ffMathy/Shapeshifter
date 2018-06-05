namespace Shapeshifter.WindowsDesktop.Data.Actions
{
	using System;
	using System.Threading.Tasks;

	using Data.Interfaces;

	using Interfaces;
	using Serilog;
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

		public string Title => "Paste";

        public byte Order => 0;

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
            await clipboardInjectionService.InjectDataAsync(package);
            await clipboardPasteService.PasteClipboardContentsAsync();

            logger.Information("Paste action performed.", 1);
        }
    }
}