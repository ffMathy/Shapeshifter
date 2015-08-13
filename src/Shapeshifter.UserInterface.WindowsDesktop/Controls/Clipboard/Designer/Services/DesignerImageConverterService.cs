using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using System.IO;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    class DesignerImageConverterService : IDesignerImageConverterService
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public DesignerImageConverterService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public BitmapSource ConvertFileBytesToBitmapSource(byte[] iconBytes)
        {
            var stream = new MemoryStream(iconBytes);

            var image = new BitmapImage();
            image.CreateOptions = BitmapCreateOptions.None;
            image.CacheOption = BitmapCacheOption.OnLoad;

            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();

            image.Freeze();

            return image;
        }

        public byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes)
        {
            var bitmapSource = ConvertFileBytesToBitmapSource(fileBytes);
            return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource);
        }
    }
}
