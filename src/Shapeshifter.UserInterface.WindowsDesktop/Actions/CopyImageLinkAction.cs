using System;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    //TODO: have custom preview generators as well. for instance, when copying ONLY an image link, generate the image itself as a preview (icon for the clipboard entry).

    class CopyImageLinkAction : ICopyImageLinkAction
    {
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
            return textData != null && HasLink(textData.Text) && ContainsImageFileExtension(textData.Text);
        }

        private bool ContainsImageFileExtension(string text)
        {
            return text.Contains(".png") || text.Contains(".jpg");
        }

        private static bool HasLink(string text)
        {
            var hints = new[] { "http://", "https://" };
            return hints.Any(text.Contains);
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
