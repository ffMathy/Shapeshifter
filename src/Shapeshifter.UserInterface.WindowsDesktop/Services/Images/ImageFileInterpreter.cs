#region

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images
{
    [ExcludeFromCodeCoverage]
    internal class ImageFileInterpreter : IImageFileInterpreter
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