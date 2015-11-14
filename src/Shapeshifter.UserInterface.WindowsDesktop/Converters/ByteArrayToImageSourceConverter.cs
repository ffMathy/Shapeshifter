using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public class ByteArrayToImageSourceConverter : InjectedConverterMarkupExtension, IValueConverter
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public ByteArrayToImageSourceConverter()
        {
        }

        public ByteArrayToImageSourceConverter(
            IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return imagePersistenceService.ConvertByteArrayToBitmapSource((byte[]) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return imagePersistenceService.ConvertBitmapSourceToByteArray((BitmapSource) value);
        }
    }
}