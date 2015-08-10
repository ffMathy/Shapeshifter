using Shapeshifter.Core.Actions;
using System;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class OpenLinkAction : IOpenLinkAction
    {
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
            var textData = clipboardData as ClipboardTextData;
            return textData != null && HasLink(textData.Text);
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
