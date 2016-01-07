namespace Shapeshifter.WindowsDesktop.Services.Images.Interfaces
{
    using System.Windows.Media.Imaging;

    using Shared.Infrastructure.Dependencies.Interfaces;

    public interface IImagePersistenceService: ISingleInstance
    {
        byte[] ConvertBitmapSourceToByteArray(BitmapSource input);

        byte[] DecorateSourceWithMetaInformation(byte[] source, ImageMetaInformation information);

        BitmapSource ConvertByteArrayToBitmapSource(byte[] input);
    }
}