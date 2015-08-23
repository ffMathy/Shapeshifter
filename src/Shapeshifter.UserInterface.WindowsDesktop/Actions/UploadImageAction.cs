using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class UploadImageAction : IUploadImageAction
    {
        readonly IFileTypeInterpreter fileTypeInterpreter;

        public UploadImageAction(
            IFileTypeInterpreter fileTypeInterpreter)
        {
            this.fileTypeInterpreter = fileTypeInterpreter;
        }

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

        public byte Order
        {
            get
            {
                return 50;
            }
        }

        public async Task<bool> CanPerformAsync(IClipboardData clipboardData)
        {
            return IsSuitableImageData(clipboardData) || IsSuitableFileData(clipboardData);
        }

        static bool IsSuitableImageData(IClipboardData clipboardData)
        {
            return clipboardData is IClipboardImageData;
        }

        bool IsSuitableFileData(IClipboardData clipboardData)
        {
            var fileData = clipboardData as IClipboardFileData;
            return fileData != null && fileTypeInterpreter.GetFileTypeFromFileName(fileData.FileName) == FileType.Image;
        }

        public Task PerformAsync(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }
    }
}
