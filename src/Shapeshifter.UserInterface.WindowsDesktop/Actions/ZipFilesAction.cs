using Shapeshifter.Core.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class ZipFilesAction : IAction
    {
        public string Description
        {
            get
            {
                return "Compress the clipboard contents into a ZIP-file and copy it.";
            }
        }

        public string Title
        {
            get
            {
                return "Copy as compressed folder";
            }
        }

        public bool CanPerform(IClipboardData clipboardData)
        {
            return clipboardData is ClipboardFileData || clipboardData is ClipboardFileCollectionData;
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
