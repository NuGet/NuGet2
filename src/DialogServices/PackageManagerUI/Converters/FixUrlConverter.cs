using System;
using System.Windows.Data;
using System.Windows;

namespace NuGet.Dialog.PackageManagerUI
{
    public class FixUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Uri source = (Uri)value;
            if (source == null || !source.IsAbsoluteUri || String.IsNullOrEmpty(source.OriginalString))
            {
                // the nuget gallery has a bug where it sends down relative path. We ignore them.
                source = null;
            }

            if (targetType == typeof(Uri))
            {
                return source;
            }
            else if (targetType == typeof(Visibility))
            {
                return source == null ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return source;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}