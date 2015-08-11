using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    class DesignerDataSourceFacade : IDataSource
    {

        private byte[] icon;

        private readonly IImagePersistenceService imagePersistenceService;

        public DesignerDataSourceFacade(
            IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        private byte[] DecorateIcon(byte[] iconBytes)
        {
            var image = ConvertFileBytesToBitmapSource(iconBytes);

            var decoratedIconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(image);
            return decoratedIconBytes;
        }

        private static BitmapSource ConvertFileBytesToBitmapSource(byte[] iconBytes)
        {
            using (var stream = new MemoryStream(iconBytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();

                image.Freeze();

                return image;
            }
        }

        public byte[] Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = DecorateIcon(value);
            }
        }

        public string Text
        {
            get; set;
        }
    }
}
