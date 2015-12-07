namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Windows.Media.Imaging;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class ImageFileInterpreter: IImageFileInterpreter
    {
        public BitmapSource Interpret(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);

            var image = new BitmapImage
            {
                CreateOptions = BitmapCreateOptions.None,
                CacheOption = BitmapCacheOption.OnLoad
            };

            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();

            image.Freeze();

            return image;
        }
    }
}