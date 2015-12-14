namespace Shapeshifter.WindowsDesktop.Services.Images
{
    using System.IO;
    using System.Windows.Media.Imaging;

    using Interfaces;

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