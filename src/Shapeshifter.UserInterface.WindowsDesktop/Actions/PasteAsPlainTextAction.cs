using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAsPlainTextAction : IPasteAsPlainTextAction
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

        public bool CanPerform(IClipboardData clipboardData)
        {
            return clipboardData is IClipboardTextData;
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
