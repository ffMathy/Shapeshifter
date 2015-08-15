using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images
{
    [ExcludeFromCodeCoverage]
    class ImageFileInterpreter : IImageFileInterpreter
    {
        public BitmapSource Interpret(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);

            var image = new BitmapImage();
            image.CreateOptions = BitmapCreateOptions.None;
            image.CacheOption = BitmapCacheOption.OnLoad;

            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();

            image.Freeze();

            return image;
        }
    }
}
