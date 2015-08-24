using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class OpenLinkAction : IOpenLinkAction
    {
        readonly ILinkParser linkParser;
        readonly IProcessManager processManager;

        public OpenLinkAction(
            ILinkParser linkParser,
            IProcessManager processManager)
        {
            this.linkParser = linkParser;
            this.processManager = processManager;
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

        public async Task<bool> CanPerformAsync(IClipboardData clipboardData)
        {
            var textData = clipboardData as IClipboardTextData;
            return textData != null && await linkParser.HasLinkAsync(textData.Text);
        }

        public async Task PerformAsync(
            IClipboardData processedData,
            IDataObject rawData)
        {
            var textData = (IClipboardTextData)processedData;
            var links = await linkParser.ExtractLinksFromTextAsync(textData.Text);
            foreach(var link in links)
            {
                processManager.StartProcess(link);
            }
        }
    }
}
