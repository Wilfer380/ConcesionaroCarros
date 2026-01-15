using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConcesionaroCarros.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;

            // Permite invertir con ConverterParameter="False"
            if (parameter?.ToString() == "False")
                flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
