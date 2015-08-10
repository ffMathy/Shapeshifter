using Shapeshifter.Core.Actions;
using System;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
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
            var textData = clipboardData as ClipboardTextData;
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
