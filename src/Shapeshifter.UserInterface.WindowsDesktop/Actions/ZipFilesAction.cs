using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;

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

        public async Task<bool> CanPerformAsync(IClipboardData clipboardData)
        {
            return clipboardData is IClipboardFileData || clipboardData is IClipboardFileCollectionData;
        }

        public Task PerformAsync(
            IClipboardData processedData)
        {
            throw new NotImplementedException();
        }
    }
}
