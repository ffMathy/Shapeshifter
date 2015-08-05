using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.IO;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ImagePersistenceService : IImagePersistenceService
    {
        public byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmapSource)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;

            using (var stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);

                var bytes = stream.ToArray();

                stream.Close();

                return bytes;
            }
        }
    }
}
