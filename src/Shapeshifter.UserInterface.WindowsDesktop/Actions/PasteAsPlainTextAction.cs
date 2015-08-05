using System;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAsPlainTextAction : IAction<ClipboardTextData>
    {
        public string Description
        {
            get
            {
                return "Pastes clipboard contents as plain text.";
            }
        }

        public string Title
        {
            get
            {
                return "Paste as plain text";
            }
        }

        public bool CanPerform(ClipboardTextData clipboardData)
        {
            return true;
        }

        public void Perform(ClipboardTextData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
