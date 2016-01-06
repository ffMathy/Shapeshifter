namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Web.Interfaces;

    using Shared.Services.Interfaces;

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

        //TODO: make the description include the links that will be opened by making a GetDescription(data) method instead.
        public string Description => "Open the link that is present in the clipboard.";

        public byte Order => 200;

        public string Title => "Open link";

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return await GetFirstSupportedItem(package)
                             .ConfigureAwait(false) != null;
        }

        async Task<IClipboardData> GetFirstSupportedItem(IClipboardDataPackage package)
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
            var textData = (IClipboardTextData) await GetFirstSupportedItem(package)
                                                          .ConfigureAwait(false);
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text)
                                        .ConfigureAwait(false);
            foreach (var link in links)
            {
                processManager.LaunchCommand(link);
            }
        }
    }
}