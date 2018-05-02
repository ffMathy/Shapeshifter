namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Files;
    using Services.Files.Interfaces;

    class UploadImageAction: IUploadImageAction
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

        public async Task<string> GetDescriptionAsync(IClipboardDataPackage package)
        {
            return "Uploads the given image to Imgur and copies a link to it.";
        }

        public string Title => "Upload image";

        public byte Order => 50;

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
			var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
			return supportedData.Any();
		}

        async Task<bool> CanPerformAsync(
            IClipboardData clipboardData)
        {
            // Until implemented
            return false;

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
            return (fileData != null) &&
                   (fileTypeInterpreter.GetFileTypeFromFileName(fileData.FileName) == FileType.Image);
        }

        public Task PerformAsync(
            IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}