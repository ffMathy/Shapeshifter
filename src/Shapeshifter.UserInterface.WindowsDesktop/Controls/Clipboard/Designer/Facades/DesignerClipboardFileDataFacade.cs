using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Windows.Media;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileDataFacade : ClipboardFileData
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public DesignerClipboardFileDataFacade(
            IImagePersistenceService imagePersistenceService) : 
            base(new DesignerFileDataSourceService(imagePersistenceService))
        {
            this.imagePersistenceService = imagePersistenceService;

            ConfigureIcon();
        }

        private void ConfigureIcon()
        {
            var metaInformation = new ImageMetaInformation()
            {
                DpiX = 96,
                DpiY = 96,
                Height = 256,
                Width = 256,
                PixelFormat = PixelFormats.Default
            };

            var decoratedIconBytes = imagePersistenceService.DecorateSourceWithMetaInformation(Resources.FileDataFileIcon, metaInformation);
            FileIcon = decoratedIconBytes;
        }
    }
}
