using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class OpenLinkAction : IOpenLinkAction
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
        public string Description
        {
            get
            {
                return "Open the link that is present in the clipboard.";
            }
        }

        public byte Order
        {
            get
            {
                return 200;
            }
        }

        public string Title
        {
            get
            {
                return "Open link";
            }
        }

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            var supportedItems = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedItems.Any();
        }

        async Task<bool> CanPerformAsync(IClipboardData data)
        {
            var textData = data as IClipboardTextData;
            return textData != null && await linkParser.HasLinkAsync(textData.Text);
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData)package;
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text);
            foreach(var link in links)
            {
                processManager.StartProcess(link);
            }
        }
    }
}
