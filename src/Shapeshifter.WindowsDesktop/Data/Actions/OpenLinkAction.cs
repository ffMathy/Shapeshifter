namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Processes.Interfaces;
    using Services.Web.Interfaces;

    class OpenLinkAction: IOpenLinkAction
    {
        readonly ILinkParser linkParser;
        readonly IProcessManager processManager;
        readonly IAsyncFilter asyncFilter;

        public OpenLinkAction(
            ILinkParser linkParser,
            IProcessManager processManager,
            IAsyncFilter asyncFilter)
        {
            this.linkParser = linkParser;
            this.processManager = processManager;
            this.asyncFilter = asyncFilter;
        }

        public async Task<string> GetTitleAsync(IClipboardDataPackage package)
		{
			return "Open links";
		}

        public byte Order => 200;

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return await GetFirstSupportedItemAsync(package)
                             .ConfigureAwait(false) != null;
        }

        async Task<IClipboardData> GetFirstSupportedItemAsync(IClipboardDataPackage package)
        {
            var supportedItems = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync)
                                                  .ConfigureAwait(false);
            return supportedItems.FirstOrDefault();
        }

        async Task<bool> CanPerformAsync(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return (textData != null) && await linkParser.HasLinkAsync(textData.Text)
                                                         .ConfigureAwait(false);
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            await DoForEveryLinkAsync(
                package,
                link => processManager.LaunchCommand(link));
        }

        async Task DoForEveryLinkAsync(IClipboardDataPackage package, Action<string> action)
        {
            var links = await ExtractLinksFromPackageAsync(package);
            foreach (var link in links)
            {
                action(link);
            }
        }

        async Task<IReadOnlyList<string>> ExtractLinksFromPackageAsync(IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedItemAsync(package)
                                                          .ConfigureAwait(false);
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text);
            return new List<string>(links);
        }
    }
}