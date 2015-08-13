using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using System;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAction : IPasteAction
    {
        public string Description
        {
            get
            {
                return "Pastes clipboard contents as-is.";
            }
        }

        public string Title
        {
            get
            {
                return "Paste";
            }
        }

        public bool CanPerform(IClipboardData clipboardData)
        {
            if(clipboardData == null)
            {
                throw new ArgumentNullException(nameof(clipboardData));
            }

            return true;
        }

        public Task PerformAsync(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
