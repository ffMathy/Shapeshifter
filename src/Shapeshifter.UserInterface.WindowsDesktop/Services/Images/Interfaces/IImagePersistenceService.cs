using Shapeshifter.UserInterface.WindowsDesktop.Services.Images;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IImagePersistenceService
    {
        byte[] ConvertBitmapSourceToByteArray(BitmapSource input);

        byte[] DecorateSourceWithMetaInformation(byte[] source, ImageMetaInformation information);

        BitmapSource ConvertByteArrayToBitmapSource(byte[] input);
    }
}
