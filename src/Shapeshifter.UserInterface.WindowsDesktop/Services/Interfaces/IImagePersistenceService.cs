using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IImagePersistenceService
    {
        byte[] ConvertBitmapSourceToByteArray(BitmapSource input);
    }
}
