using System;
using System.Windows.Data;

namespace NuGet.WebMatrix
{
    public class FixUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Uri source = (Uri)value;
            if (source == null || !source.IsAbsoluteUri)
            {
                // the nuget gallery has a bug where it sends down relative path. We ignore them.
                return null;
            }

            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
