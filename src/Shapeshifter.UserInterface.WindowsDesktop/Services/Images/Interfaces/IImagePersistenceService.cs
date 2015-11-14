using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces
{
    public interface IImagePersistenceService : ISingleInstance
    {
        byte[] ConvertBitmapSourceToByteArray(BitmapSource input);

        byte[] DecorateSourceWithMetaInformation(byte[] source, ImageMetaInformation information);

        BitmapSource ConvertByteArrayToBitmapSource(byte[] input);
    }
}
