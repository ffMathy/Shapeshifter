using Shapeshifter.Core.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class OpenLinkAction : IAction
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
