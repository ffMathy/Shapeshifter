using System;
using Shapeshifter.Core.Actions;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class PasteAsPlainTextAction : IAction
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
            return clipboardData is ClipboardTextData;
        }

        public void Perform(IClipboardData clipboardData)
        {
            var textData = (ClipboardTextData)clipboardData;

            throw new NotImplementedException();
        }
    }
}
