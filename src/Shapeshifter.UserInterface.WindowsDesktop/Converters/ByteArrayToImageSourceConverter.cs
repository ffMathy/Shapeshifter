using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bytes = (byte[])value;
            if (bytes == null) return null;

            var image = new BitmapImage();
            using (var stream = new MemoryStream(bytes))
            {
                stream.Position = 0;

                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.None;
                image.CacheOption = BitmapCacheOption.OnDemand;
                image.UriSource = null;
                image.StreamSource = stream;
                image.EndInit();
            }

            image.Freeze();

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
