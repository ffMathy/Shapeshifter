namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
    using Services.Files.Interfaces;

    public class CopyAsFileAction: ICopyAsFileAction
    {
        readonly IFileManager fileManager;
        readonly IClipboardInjectionService clipboardInjectionService;

        public string Title
            => "Copy as file";

        public async Task<string> GetDescriptionAsync(IClipboardDataPackage package)
        {
            return "Copy this text to the clipboard as a file.";
        }

        public byte Order
            => 20;

        public CopyAsFileAction(
            IFileManager fileManager,
            IClipboardInjectionService clipboardInjectionService)
        {
            this.fileManager = fileManager;
            this.clipboardInjectionService = clipboardInjectionService;
        }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return GetFirstSupportedItem(package) != null;
        }

        static IClipboardTextData GetFirstSupportedItem(IClipboardDataPackage package)
        {
            var supportedItems = package
                .Contents
                .Where(CanPerform)
                .Cast<IClipboardTextData>();
            return supportedItems.FirstOrDefault();
        }

        static bool CanPerform(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return textData != null;
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            var supportedItem = GetFirstSupportedItem(package);

            var randomFileName = DateTime.Now.Ticks;
            var randomFilePath = fileManager.WriteBytesToTemporaryFile(
                $"{randomFileName}.txt", 
                Encoding.UTF8.GetBytes(supportedItem.Text));

            clipboardInjectionService.InjectFiles(
                randomFilePath);
        }
    }
}