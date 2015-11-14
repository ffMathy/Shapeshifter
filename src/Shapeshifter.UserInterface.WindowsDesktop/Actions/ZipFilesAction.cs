#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    internal class ZipFilesAction : IZipFilesAction
    {
        private readonly IAsyncFilter asyncFilter;

        public ZipFilesAction(IAsyncFilter asyncFilter)
        {
            this.asyncFilter = asyncFilter;
        }

        public string Description
        {
            get { return "Compress the clipboard contents into a ZIP-file and copy it."; }
        }

        public byte Order
        {
            get { return 75; }
        }

        public string Title
        {
            get { return "Copy as compressed folder"; }
        }

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData.Any();
        }

        private async Task<bool> CanPerformAsync(
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