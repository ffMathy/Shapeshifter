using System;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class UploadImageAction : IUploadImageAction
    {
        readonly IFileTypeInterpreter fileTypeInterpreter;
        readonly IAsyncFilter asyncFilter;

        public UploadImageAction(
            IFileTypeInterpreter fileTypeInterpreter,
            IAsyncFilter asyncFilter)
        {
            this.fileTypeInterpreter = fileTypeInterpreter;
            this.asyncFilter = asyncFilter;
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

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData.Any();
        }

        async Task<bool> CanPerformAsync(
            IClipboardData clipboardData)
        {
            return IsSuitableImageData(clipboardData) || IsSuitableFileData(clipboardData);
        }

        static bool IsSuitableImageData(
            IClipboardData clipboardData)
        {
            return clipboardData is IClipboardImageData;
        }

        bool IsSuitableFileData(
            IClipboardData clipboardData)
        {
            var fileData = clipboardData as IClipboardFileData;
            return fileData != null && fileTypeInterpreter.GetFileTypeFromFileName(fileData.FileName) == FileType.Image;
        }

        public Task PerformAsync(
            IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}
