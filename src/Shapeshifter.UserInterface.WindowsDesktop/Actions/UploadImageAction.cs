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
                return "Uploads the given to Imgur image and copies a link to it.";
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
            return IsSuitableImageData(clipboardData);
        }

        private static bool IsSuitableImageData(IClipboardData clipboardData)
        {
            return clipboardData is ClipboardImageData;
        }

        private static bool IsSuitableFileData(IClipboardData clipboardData)
        {
            var fileData = clipboardData as ClipboardFileData;
            return fileData != null && HasImageFileExtension(fileData.FileName);
        }

        private static bool HasImageFileExtension(string filename)
        {
            var hints = new[] { ".png", ".jpg" };
            return hints.Any(filename.EndsWith);
        }

        public void Perform(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
