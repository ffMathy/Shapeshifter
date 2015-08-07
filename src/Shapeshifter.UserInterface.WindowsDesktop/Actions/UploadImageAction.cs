using Shapeshifter.Core.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class UploadImageAction : IAction
    {
        public string Description
        {
            get
            {
                return "Uploads the given image and copies a link to it.";
            }
        }

        public string Title
        {
            get
            {
                return "Upload image";
            }
        }

        public bool CanPerform(IClipboardData clipboardData)
        {
            return clipboardData is ClipboardImageData;
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
