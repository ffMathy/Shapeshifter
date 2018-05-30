namespace Shapeshifter.WindowsDesktop.Data.Actions
{
	using System;
	using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    using Data.Interfaces;

	using FluffySpoon.Http;

	using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
    using Services.Images.Interfaces;
    using Services.Web;
    using Services.Web.Interfaces;

    class CopyImageLinkAction: ICopyImageLinkAction
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

        public async Task<string> GetTitleAsync(IClipboardDataPackage package)
        {
			return "Copy images from links";
        }

        public byte Order => 100;

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return await GetFirstSupportedDataAsync(package)
                             .ConfigureAwait(false) != null;
        }

        async Task<IClipboardData> GetFirstSupportedDataAsync(IClipboardDataPackage package)
        {
            var validItems = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync)
                                              .ConfigureAwait(false);
            return validItems.FirstOrDefault();
        }

        async Task<bool> CanPerformAsync(IClipboardData data)
        {
	    // Until implemented
	    return false;
	    
		//	return data is IClipboardTextData textData &&
                //   await linkParser.HasLinkOfTypeAsync(textData.Text, LinkType.ImageFile)
                //                   .ConfigureAwait(false);
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            var links = await ExtractLinksFromPackageAsync(package);

            var imagesBytes = await DownloadLinksAsync(links)
                                        .ConfigureAwait(false);

            var images = InterpretImages(imagesBytes);
            await InjectImagesAsync(images);
        }

        async Task<IReadOnlyList<string>> ExtractLinksFromPackageAsync(IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedDataAsync(package)
                                                          .ConfigureAwait(false);
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text)
                                        .ConfigureAwait(false);
            return new List<string>(links);
        }

        IEnumerable<BitmapSource> InterpretImages(IEnumerable<byte[]> imagesBytes)
        {
            return imagesBytes.Select(imageFileInterpreter.Interpret);
        }

        async Task InjectImagesAsync(IEnumerable<BitmapSource> images)
        {
            foreach (var image in images)
            {
                await clipboardInjectionService.InjectImageAsync(image);
            }
        }

        async Task<IEnumerable<byte[]>> DownloadLinksAsync(IEnumerable<string> links)
        {
            var downloadTasks = new List<Task<byte[]>>();
            foreach (var link in links)
            {
                downloadTasks.Add(downloader.DownloadBytesAsync(new Uri(link)));
            }

            await Task.WhenAll(downloadTasks)
                      .ConfigureAwait(false);

            return downloadTasks.Select(x => x.Result);
        }
    }
}
