using System;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class UploadImageAction : IUploadImageAction
    {
        private readonly IFileTypeInterpreter fileTypeInterpreter;
        private readonly IAsyncFilter asyncFilter;

        public UploadImageAction(
            IFileTypeInterpreter fileTypeInterpreter,
            IAsyncFilter asyncFilter)
        {
            this.fileTypeInterpreter = fileTypeInterpreter;
            this.asyncFilter = asyncFilter;
        }

        public string Description => "Uploads the given image to Imgur and copies a link to it.";

        public string Title => "Upload image";

        public byte Order => 50;

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData.Any();
        }

        private async Task<bool> CanPerformAsync(
            IClipboardData clipboardData)
        {
            return IsSuitableImageData(clipboardData) || IsSuitableFileData(clipboardData);
        }

        private static bool IsSuitableImageData(
            IClipboardData clipboardData)
        {
            return clipboardData is IClipboardImageData;
        }

        private bool IsSuitableFileData(
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