using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class OpenLinkAction : IOpenLinkAction
    {
        private readonly ILinkParser linkParser;
        private readonly IProcessManager processManager;
        private readonly IAsyncFilter asyncFilter;

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
            return await GetFirstSupportedItem(package).ConfigureAwait(false) != null;
        }

        private async Task<IClipboardData> GetFirstSupportedItem(IClipboardDataPackage package)
        {
            var supportedItems = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync).ConfigureAwait(false);
            return supportedItems.FirstOrDefault();
        }

        private async Task<bool> CanPerformAsync(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return textData != null && await linkParser.HasLinkAsync(textData.Text).ConfigureAwait(false);
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedItem(package).ConfigureAwait(false);
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text).ConfigureAwait(false);
            foreach (var link in links)
            {
                processManager.LaunchCommand(link);
            }
        }
    }
}