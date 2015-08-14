using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class OpenLinkAction : IOpenLinkAction
    {
        private readonly ILinkParser linkParser;
        private readonly IProcessManager processManager;

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

        public string Title
        {
            get
            {
                return "Open link";
            }
        }

        public bool CanPerform(IClipboardData clipboardData)
        {
            var textData = clipboardData as IClipboardTextData;
            return textData != null && linkParser.HasLink(textData.Text);
        }

        public async Task PerformAsync(IClipboardData clipboardData)
        {
            var textData = (IClipboardTextData)clipboardData;
            var links = linkParser.ExtractLinksFromText(textData.Text);
            foreach(var link in links)
            {
                processManager.StartProcess(link);
            }
        }
    }
}
