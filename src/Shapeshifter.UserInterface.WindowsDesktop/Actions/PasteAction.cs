using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Data;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAction : IAction
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
            return true;
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
