namespace Shapeshifter.WindowsDesktop.Controls.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class BrushCloneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Freezable)
            {
                value = ((Freezable) value).Clone();
            }
            
            return value;

        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();

        }
    }
}