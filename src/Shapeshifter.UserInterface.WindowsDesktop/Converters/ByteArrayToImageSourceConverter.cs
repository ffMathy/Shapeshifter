using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public ByteArrayToImageSourceConverter()
        {
            this.imagePersistenceService = App.Container.Resolve<IImagePersistenceService>();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return imagePersistenceService.ConvertByteArrayToBitmapSource((byte[])value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return imagePersistenceService.ConvertBitmapSourceToByteArray((BitmapSource)value);
        }
    }
}
