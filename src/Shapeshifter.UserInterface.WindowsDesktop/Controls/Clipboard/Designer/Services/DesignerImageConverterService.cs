using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using System.IO;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    class DesignerImageConverterService : IDesignerImageConverterService
    {
        public BitmapSource ConvertFileBytesToBitmapSource(byte[] iconBytes)
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
    }
}
