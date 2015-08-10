using System;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class UploadImageAction : IUploadImageAction
    {
        public string Description
        {
            get
            {
                return "Uploads the given image to Imgur and copies a link to it.";
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
            return IsSuitableImageData(clipboardData) || IsSuitableFileData(clipboardData);
        }

        private static bool IsSuitableImageData(IClipboardData clipboardData)
        {
            return clipboardData is IClipboardImageData;
        }

        private static bool IsSuitableFileData(IClipboardData clipboardData)
        {
            var fileData = clipboardData as IClipboardFileData;
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
