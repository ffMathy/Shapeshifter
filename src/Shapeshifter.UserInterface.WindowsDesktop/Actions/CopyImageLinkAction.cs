using System;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    //TODO: have custom preview generators as well. for instance, when copying ONLY an image link, generate the image itself as a preview (icon for the clipboard entry).

    class CopyImageLinkAction : ICopyImageLinkAction
    {
        private readonly ILinkParser linkParser;

        public CopyImageLinkAction(ILinkParser linkParser)
        {
            this.linkParser = linkParser;
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

        public bool CanPerform(IClipboardData clipboardData)
        {
            var textData = clipboardData as IClipboardTextData;
            return textData != null && linkParser.HasLinkOfType(textData.Text, LinkType.ImageFile);
        }

        public void Perform(IClipboardData clipboardData)
        {
            var textData = clipboardData as IClipboardTextData;
            

            throw new NotImplementedException();
        }
    }
}
