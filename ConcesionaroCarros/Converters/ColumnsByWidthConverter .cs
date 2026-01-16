using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcesionaroCarros.Converters
{
    public class ColumnsByWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // breakpoint realista
                return width < 1000 ? 1 : 2;
            }
            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
