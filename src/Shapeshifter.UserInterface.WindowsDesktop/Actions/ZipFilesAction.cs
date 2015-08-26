using System;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    class ZipFilesAction : IZipFilesAction
    {
        public string Description
        {
            get
            {
                return "Compress the clipboard contents into a ZIP-file and copy it.";
            }
        }

        public byte Order
        {
            get
            {
                return 75;
            }
        }

        public string Title
        {
            get
            {
                return "Copy as compressed folder";
            }
        }

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            return 
                package is IClipboardFileData || 
                package is IClipboardFileCollectionData;
        }

        public Task PerformAsync(
            IClipboardDataPackage processedData)
        {
            throw new NotImplementedException();
        }
    }
}
