using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    //TODO: have custom preview generators as well. for instance, when copying ONLY an image link, generate the image itself as a preview (icon for the clipboard entry).

    internal class CopyImageLinkAction : ICopyImageLinkAction
    {
        private readonly IClipboardInjectionService clipboardInjectionService;
        private readonly IDownloader downloader;
        private readonly IImageFileInterpreter imageFileInterpreter;
        private readonly ILinkParser linkParser;
        private readonly IAsyncFilter asyncFilter;

        public CopyImageLinkAction(
            ILinkParser linkParser,
            IImageFileInterpreter imageFileInterpreter,
            IDownloader downloader,
            IClipboardInjectionService clipboardInjectionService,
            IAsyncFilter asyncFilter)
        {
            this.asyncFilter = asyncFilter;
            this.linkParser = linkParser;
            this.downloader = downloader;
            this.imageFileInterpreter = imageFileInterpreter;
            this.clipboardInjectionService = clipboardInjectionService;
        }

        public string Description => "Copies the image that the link in the clipboard text points to.";

        public string Title => "Copy image from link";

        public byte Order => 100;

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return await GetFirstSupportedDataAsync(package).ConfigureAwait(false) != null;
        }

        private async Task<IClipboardData> GetFirstSupportedDataAsync(IClipboardDataPackage package)
        {
            var validItems = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync).ConfigureAwait(false);
            return validItems.FirstOrDefault();
        }

        private async Task<bool> CanPerformAsync(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return textData != null &&
                   await linkParser.HasLinkOfTypeAsync(textData.Text, LinkType.ImageFile).ConfigureAwait(false);
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedDataAsync(package).ConfigureAwait(false);
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text).ConfigureAwait(false);

            var imagesBytes = await DownloadLinksAsync(links).ConfigureAwait(false);

            var images = InterpretImages(imagesBytes);
            InjectImages(images);
        }

        private IEnumerable<BitmapSource> InterpretImages(IEnumerable<byte[]> imagesBytes)
        {
            return imagesBytes.Select(imageFileInterpreter.Interpret);
        }

        private void InjectImages(IEnumerable<BitmapSource> images)
        {
            foreach (var image in images)
            {
                clipboardInjectionService.InjectImage(image);
            }
        }

        private async Task<IEnumerable<byte[]>> DownloadLinksAsync(IEnumerable<string> links)
        {
            var downloadTasks = new List<Task<byte[]>>();
            foreach (var link in links)
            {
                downloadTasks.Add(downloader.DownloadBytesAsync(link));
            }

            await Task.WhenAll(downloadTasks).ConfigureAwait(false);

            return downloadTasks.Select(x => x.Result);
        }
    }
}