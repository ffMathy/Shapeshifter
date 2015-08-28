using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    //TODO: have custom preview generators as well. for instance, when copying ONLY an image link, generate the image itself as a preview (icon for the clipboard entry).

    class CopyImageLinkAction : ICopyImageLinkAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;
        readonly IDownloader downloader;
        readonly IImageFileInterpreter imageFileInterpreter;
        readonly ILinkParser linkParser;
        readonly IAsyncFilter asyncFilter;

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

        public string Description
        {
            get
            {
                return "Copies the image that the link in the clipboard text points to.";
            }
        }

        public string Title
        {
            get
            {
                return "Copy image from link";
            }
        }

        public byte Order
        {
            get
            {
                return 100;
            }
        }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return GetFirstSupportedDataAsync(package) != null;
        }

        async Task<IClipboardData> GetFirstSupportedDataAsync(IClipboardDataPackage package)
        {
            var validItems = await asyncFilter.FilterAsync(package.Contents, PerformAsync);
            return validItems.FirstOrDefault();
        }

        async Task<bool> PerformAsync(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return textData != null && await linkParser.HasLinkOfTypeAsync(textData.Text, LinkType.ImageFile);
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            var textData = package as IClipboardTextData;
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text);

            var imagesBytes = await DownloadLinksAsync(links);

            var images = InterpretImages(imagesBytes);
            InjectImages(images);
        }

        IEnumerable<BitmapSource> InterpretImages(IEnumerable<byte[]> imagesBytes)
        {
            return imagesBytes.Select(imageFileInterpreter.Interpret);
        }

        void InjectImages(IEnumerable<BitmapSource> images)
        {
            foreach (var image in images)
            {
                clipboardInjectionService.InjectImage(image);
            }
        }

        async Task<IEnumerable<byte[]>> DownloadLinksAsync(IEnumerable<string> links)
        {
            var downloadTasks = new List<Task<byte[]>>();
            foreach (var link in links)
            {
                downloadTasks.Add(downloader.DownloadBytesAsync(link));
            }

            await Task.WhenAll(downloadTasks);

            return downloadTasks.Select(x => x.Result);
        }
    }
}
