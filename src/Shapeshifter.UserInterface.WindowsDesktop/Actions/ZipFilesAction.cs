using System;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class ZipFilesAction : IZipFilesAction
    {
        private readonly IAsyncFilter asyncFilter;

        public ZipFilesAction(IAsyncFilter asyncFilter)
        {
            this.asyncFilter = asyncFilter;
        }

        public string Description => "Compress the clipboard contents into a ZIP-file and copy it.";

        public byte Order => 75;

        public string Title => "Copy as compressed folder";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData.Any();
        }

        private static async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return
                data is IClipboardFileData ||
                data is IClipboardFileCollectionData;
        }

        public Task PerformAsync(
            IClipboardDataPackage processedData)
        {
            throw new NotImplementedException();
        }
    }
}