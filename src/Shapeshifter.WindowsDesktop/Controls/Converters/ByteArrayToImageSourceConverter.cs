namespace Shapeshifter.WindowsDesktop.Controls.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    using Services.Images.Interfaces;

    public class ByteArrayToImageSourceConverter
        : InjectedConverterMarkupExtension,
          IValueConverter
    {
        readonly IImagePersistenceService imagePersistenceService;

        public ByteArrayToImageSourceConverter() { }

        public ByteArrayToImageSourceConverter(
            IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return imagePersistenceService.ConvertByteArrayToBitmapSource((byte[]) value);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return imagePersistenceService.ConvertBitmapSourceToByteArray((BitmapSource) value);
        }
    }
}